import { signal } from '@angular/core';
import { commandId } from './command-id';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { PublicGap, TyreCompound } from '../ui/timing/public-classification.model';
import { Pace, Battle } from '../ui/controller/controller.model';
export interface PublicState {
    sessionId: string;
    phase: number;
    revision: number;
    lifecycle: string;
    leaderLap: number;
    totalLaps: number;
    classification: {
        driverId: string;
        name: string;
        teamId: string;
        position: number;
        gapToLeader: PublicGap;
        gapToAhead: PublicGap;
        tyreAge: number;
        compound: TyreCompound;
        pitState: string;
        completedLaps: number;
        eligibleAtFinish: boolean | null;
    }[];
    events: {
        id: string;
        kind: string;
        driverId: string;
        lap: number;
        otherDriverId: string | null;
        compound: TyreCompound | null;
    }[];
    presentation: { cursor: string; resolvedPhase:number; totalEvents: number; overtakes: number; pitEntries: number; pitExits: number; tyreChanges: number; omittedEvents: number; summary: string } | null;
}
interface Command {
    commandId: string;
    phase: number;
    revision: number;
    cars: {
        driverId: string;
        pace: Pace;
        pit: string;
        compound: string | null;
        battle: Battle;
    }[];
}
interface TeamState {
    sessionId: string;
    phase: number;
    revision: number;
    lifecycle: string;
    confirmedPlan: Command | null;
    cars: {
        driverId: string;
        compound: TyreCompound;
        tyreAge: number;
        estimatedTyreCondition: number;
        inPits: boolean;
        stopQueued: boolean;
        battleOptions: Battle[];
    }[];
}
interface Acknowledgement {
    commandId: string;
    sessionId: string;
    phase: number;
    revision: number;
    lifecycle: string;
}
class RequestFailure extends Error {
    constructor(readonly status: number, message: string) { super(message); }
}
interface Choice {
    driverId: string;
    pace: Pace;
    pit: string;
    compound: string;
    battle: Battle;
}
interface Created {
    sessionId: string;
    displayUrl: string;
    controllerUrl: string;
}
export class RaceSession {
    readonly mode: string;
    readonly id: string;
    readonly capability: string;
    readonly controllerLink: string | null;
    readonly created = signal<Created | null>(null);
    readonly publicState = signal<PublicState | null>(null);
    readonly team = signal<TeamState | null>(null);
    readonly error = signal('');
    readonly busy = signal(false);
    readonly choices = signal<Choice[]>([]);
    readonly connectionState = signal<'connecting' | 'connected' | 'disconnected' | 'unavailable'>('connecting');
    readonly pending = signal<Command | null>(null);
    readonly acknowledged = signal<Acknowledgement | null>(null);
    private connection?: HubConnection;
    constructor(url: URL) {
        this.mode = url.pathname.split('/')[2];
        this.id = url.pathname.split('/')[3];
        this.capability = new URLSearchParams(url.hash.slice(1)).get('capability') ?? '';
        const saved = localStorage.getItem('race-controller:' + this.id);
        this.controllerLink = saved && saved.startsWith('/race/controller/' + this.id + '#capability=') ? saved : null;
        if (this.mode === 'controller' && this.capability)
            localStorage.setItem('race-controller:' + this.id, url.pathname + url.hash);
        if (this.mode !== 'new')
            void this.connect();
    }
    async create(preset: string, laps: string, seed: string) {
        this.error.set('');
        try {
            this.rememberCreated(await this.request<Created>('/api/sessions', { preset, laps: Number(laps), seed: Number(seed) }));
        }
        catch (error) {
            this.fail(error);
        }
    }
    async connect() {
        this.connectionState.set('connecting');
        this.error.set('');
        const previous = this.connection;
        this.connection = undefined;
        await previous?.stop();
        const connection = new HubConnectionBuilder().withUrl('/hubs/race').withAutomaticReconnect([0, 1000, 3000]).build();
        this.connection = connection;
        connection.on('publicSnapshot', (state: PublicState) => this.acceptPublic(state));
        connection.on('teamSnapshot', (state: TeamState) => this.acceptTeam(state));
        connection.onreconnecting(() => {
            if (this.connection === connection)
                this.connectionState.set('connecting');
        });
        connection.onreconnected(async () => {
            if (this.connection !== connection)
                return;
            try {
                await this.rejoin(connection);
            }
            catch (error) {
                this.connectionFailure(error);
            }
        });
        connection.onclose(() => {
            if (this.connection === connection && this.connectionState() !== 'unavailable')
                this.connectionState.set('disconnected');
        });
        try {
            await connection.start();
            await this.rejoin(connection);
        }
        catch (error) {
            this.connectionFailure(error);
        }
    }
    private async rejoin(connection: HubConnection) {
        // Groups belong to this connection. Revalidate, join, then retrieve fresh authoritative state.
        const publicState = await connection.invoke<PublicState>('SubscribePublic', this.id);
        if (this.connection !== connection)
            return;
        this.acceptPublic(publicState);
        if (this.mode === 'controller') {
            const own = await connection.invoke<TeamState>('SubscribeTeam', this.id, this.capability);
            if (this.connection !== connection)
                return;
            this.acceptTeam(own);
        }
        this.connectionState.set('connected');
        this.error.set('');
    }
    private acceptPublic(state: PublicState) {
        if (state.sessionId !== this.id || state.revision <= (this.publicState()?.revision ?? -1))
            return;
        this.publicState.set(state);
    }
    private acceptTeam(state: TeamState) {
        if (state.sessionId !== this.id || state.revision <= (this.team()?.revision ?? -1))
            return;
        if (state.phase !== this.team()?.phase) {
            this.acknowledged.set(null);
            this.pending.set(null);
            this.choices.set(state.cars.map(car => ({ driverId: car.driverId, pace: 'Standard', pit: 'StayOut', compound: 'Hard', battle: 'Neutral' })));
        }
        if (state.confirmedPlan) {
            this.choices.set(state.confirmedPlan.cars.map(car => ({ ...car, pit: car.pit === 'Pit' ? 'Pit' : 'StayOut', compound: car.compound ?? 'Hard' })));
            this.pending.set(null);
        }
        else if (!this.team())
            this.choices.set(state.cars.map(car => ({ driverId: car.driverId, pace: 'Standard', pit: 'StayOut', compound: 'Hard', battle: 'Neutral' })));
        this.team.set(state);
    }
    private canSubmit() { return !this.busy() && !this.acknowledged() && this.connectionState() === 'connected' && this.team()?.lifecycle === 'awaiting-decisions'; }
    editable() { return this.canSubmit() && !this.pending(); }
    canRetry() { return this.canSubmit() && !!this.pending(); }
    async restart() {
        this.error.set('');
        try { this.rememberCreated(await this.request<Created>('/api/sessions/' + this.id + '/restart', {}, this.capability)); }
        catch (error) { this.fail(error); }
    }
    async submit() {
        const team = this.team();
        if (!team || !this.canSubmit())
            return;
        const command = this.pending() ?? { commandId: commandId(), phase: team.phase, revision: team.revision, cars: this.choices().map(car => ({ ...car, pit: car.pit === 'Pit' ? 'Pit' : 'Stay Out', compound: car.pit === 'Pit' ? car.compound : null })) };
        this.pending.set(command);
        this.busy.set(true);
        this.error.set('');
        try {
            const ack = await this.request<Acknowledgement>('/api/sessions/' + this.id + '/plans', command, this.capability);
            if (this.team()?.phase !== command.phase) return;
            if (ack.sessionId === this.id && ack.commandId === command.commandId) {
                this.acknowledged.set(ack);
                this.pending.set(null);
                // An acknowledgement proves the lock, not current tyre observations. Fetch complete fresh snapshots.
                if (this.connection) {
                    try {
                        await this.rejoin(this.connection);
                    }
                    catch (error) {
                        this.connectionFailure(error);
                    }
                }
            }
        }
        catch (error) {
            if (this.team()?.phase !== command.phase) return;
            if (this.team()?.confirmedPlan?.commandId === command.commandId)
                return;
            if (error instanceof RequestFailure && error.status >= 400 && error.status < 500) {
                this.pending.set(null);
                this.fail(error);
                if (error.status === 409 && this.connection) {
                    try {
                        await this.rejoin(this.connection);
                    }
                    catch (failure) {
                        this.connectionFailure(failure);
                    }
                }
            }
            else {
                this.connectionState.set('disconnected');
                this.error.set('Submission confirmation unavailable. Reconnect to check the server, then retry the same pending plan if needed.');
                await this.connection?.stop();
            }
        }
        finally {
            this.busy.set(false);
        }
    }
    private rememberCreated(value: Created) {
        const id = value.displayUrl.split('/').at(-1)!;
        localStorage.setItem('race-controller:' + id, value.controllerUrl);
        this.created.set(value);
    }
    private async request<T>(url: string, body: unknown, capability?: string): Promise<T> {
        const response = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json', ...(capability ? { Authorization: 'Bearer ' + capability } : {}) }, body: JSON.stringify(body) });
        const value = await response.json();
        if (!response.ok)
            throw new RequestFailure(response.status, value.message ?? 'Request failed');
        return value;
    }
    private connectionFailure(error: unknown) { this.fail(error); this.connectionState.set(this.error().includes('unavailable') || this.error().includes('capability') ? 'unavailable' : 'disconnected'); }
    private fail(error: unknown) { this.error.set(error instanceof Error ? error.message : 'Unable to connect. Create a fresh session if the server restarted.'); }
    dispose() { const connection = this.connection; this.connection = undefined; void connection?.stop(); }
}
