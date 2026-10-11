import { signal } from '@angular/core';
import { commandId } from './command-id';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { PublicGap, TyreCompound } from '../ui/timing/public-classification.model';
import { Pace, Battle } from '../ui/controller/controller.model';
export interface PublicState {
    paused?:boolean;
    decisionDeadlineUtc?:string|null;
    pausedRemainingSeconds?:number|null;
    serverTimeUtc?:string;
    timingPolicy?:{effectiveSeconds:number;timeoutPolicy:string}|null;
    decisionSources?:{teamId:string;source:string}[];
    readiness: {teamId:string;locked:boolean}[] | null;
    sessionId: string;
    phase: number;
    revision: number;
    lifecycle: string;
    leaderLap: number;
    totalLaps: number;
    nextDecisionLaps: number;
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
        recentLapTimes: {lap:number;seconds:number}[];
        completedPitStops:number;
        isFastestLap:boolean;
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
    teamId:string; assignmentRevision:number; commandRevision:number;
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
interface ControllerAssignment {teamId:string;constructorName:string;control:string;assignmentRevision:number;playerId:string}
export class RaceSession {
    readonly mode: string;
    readonly id: string;
    readonly capability: string;
    readonly controllerLink: string | null;
    householdTeam: string;
    readonly sharedTablet: boolean;
    readonly sharedTeams = signal<ControllerAssignment[]>([]);
    readonly nextTeam = signal('');
    readonly handoffMessage = signal('');
    readonly activePrivate = signal(false);
    private privateGeneration = 0;
    private assignmentRequest = 0;
    private householdDevice = "";
    private householdUser?: import("firebase/auth").User;
    readonly created = signal<Created | null>(null);
    readonly remainingDecisionSeconds=signal<number|null>(null);
    private serverClockOffset=0;
    private countdown=setInterval(()=>this.updateCountdown(),250);
    private updateCountdown(){const state=this.publicState();this.remainingDecisionSeconds.set(state?.pausedRemainingSeconds??(state?.decisionDeadlineUtc?Math.max(0,Math.ceil((Date.parse(state.decisionDeadlineUtc)-Date.now()-this.serverClockOffset)/1000)):null));}
    decisionCountdown(){const seconds=this.remainingDecisionSeconds();return seconds===null?'':Math.floor(seconds/60)+':'+String(seconds%60).padStart(2,'0');}
    publicTeamName(teamId:string){return 'Team '+teamId.replace('team-','');}
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
        this.householdTeam = url.searchParams.get("team") ?? "";
        this.sharedTablet = url.searchParams.get('shared') === '1';
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
        if(this.sharedTablet){await this.passTablet(false);this.handoffMessage.set('Private view cleared. Hand over the tablet, then press Ready.');}
        this.connectionState.set('connecting');
        this.error.set('');
        const previous = this.connection;
        this.connection = undefined;
        await previous?.stop();
        if (this.householdTeam || this.sharedTablet) {
          try {
            const config = await (await fetch('/api/auth/config')).json();
            const {initializeApp,getApps}=await import('firebase/app'); const sdk=await import('firebase/auth');
            const auth=sdk.getAuth(getApps()[0] ?? initializeApp({projectId:config.projectId,apiKey:config.apiKey}));
            if(config.emulatorUrl && !auth.emulatorConfig)sdk.connectAuthEmulator(auth,config.emulatorUrl,{disableWarnings:true});
            await auth.authStateReady(); this.householdUser=auth.currentUser ?? undefined;
            if(!this.householdUser)throw new Error('Sign in and join this household first.');
            this.householdDevice=localStorage.getItem('household-device:'+this.householdUser.uid+':'+this.id) ?? '';
            await this.refreshAssignments();
            if(this.sharedTablet){this.householdTeam='';this.activePrivate.set(false);}
          } catch(error) { this.connectionFailure(error); return; }
        }
        const connection = new HubConnectionBuilder().withUrl('/hubs/race', this.householdUser ? {accessTokenFactory:()=>this.householdUser!.getIdToken()} : {}).withAutomaticReconnect([0, 1000, 3000]).build();
        this.connection = connection;
        connection.on('publicSnapshot', (state: PublicState) => this.acceptPublic(state));
        connection.on('teamSnapshot', (state: TeamState) => this.acceptTeam(state));
        connection.on('lobbyRevision', () => { if(this.householdUser)void this.updateAuthority().catch(error=>this.connectionFailure(error)); });
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
        if (this.mode === 'controller' && (!this.sharedTablet || this.activePrivate())) {
            const generation=this.privateGeneration;
            const teamId=this.householdTeam;
            if(this.householdUser)await this.refreshAssignments();
            const own = this.householdTeam ? await connection.invoke<TeamState>('SubscribeHouseholdTeam',this.id,this.householdDevice,this.householdTeam) : await connection.invoke<TeamState>('SubscribeTeam', this.id, this.capability);
            if (this.connection !== connection || generation!==this.privateGeneration || teamId!==this.householdTeam){
                if(teamId)await connection.invoke('UnsubscribeHouseholdTeam',this.id,teamId,own.assignmentRevision).catch(()=>undefined);
                return;
            }
            this.acceptTeam(own);
        }
        this.connectionState.set('connected');
        this.error.set('');
    }
    private acceptPublic(state: PublicState) {
        if (state.sessionId !== this.id || state.revision <= (this.publicState()?.revision ?? -1))
            return;
        if(state.serverTimeUtc)this.serverClockOffset=Date.parse(state.serverTimeUtc)-Date.now();
        this.publicState.set(state);this.updateCountdown();
        if(this.sharedTablet && !this.activePrivate())this.selectNextTeam();
    }
    private acceptTeam(state: TeamState) {
        const submitted = this.pending();
        const previous = this.team();
        const assignment=this.sharedTeams().find(team=>team.teamId===this.householdTeam);
        if(this.sharedTablet && state.revision<(this.publicState()?.revision??-1))return;
        if (state.sessionId !== this.id || this.householdTeam && !assignment || this.sharedTablet && !this.activePrivate() || this.householdTeam && state.teamId !== this.householdTeam || assignment && state.assignmentRevision !== assignment.assignmentRevision || state.assignmentRevision < (this.team()?.assignmentRevision ?? -1) || state.assignmentRevision === this.team()?.assignmentRevision && state.revision <= (this.team()?.revision ?? -1))
            return;
        if (state.phase !== this.team()?.phase || state.assignmentRevision !== this.team()?.assignmentRevision) {
            this.acknowledged.set(null);
            this.pending.set(null);
            this.busy.set(false);
            this.choices.set(state.cars.map(car => ({ driverId: car.driverId, pace: 'Standard', pit: 'StayOut', compound: 'Hard', battle: 'Neutral' })));
        }
        if (state.confirmedPlan) {
            this.choices.set(state.confirmedPlan.cars.map(car => ({ ...car, pit: car.pit === 'Pit' ? 'Pit' : 'StayOut', compound: car.compound ?? 'Hard' })));
            this.pending.set(null);
        }
        else if (!this.team())
            this.choices.set(state.cars.map(car => ({ driverId: car.driverId, pace: 'Standard', pit: 'StayOut', compound: 'Hard', battle: 'Neutral' })));
        this.team.set(state);
        const newlyLocked = previous && !previous.confirmedPlan && (state.confirmedPlan || state.phase!==previous.phase);
        if(this.sharedTablet && (newlyLocked || submitted && state.phase!==submitted.phase))void this.passTablet(false);
    }
    private canSubmit() { return (!!this.pending() || this.remainingDecisionSeconds()!==0) && !this.busy() && !this.acknowledged() && this.connectionState() === 'connected' && this.team()?.lifecycle === 'awaiting-decisions' && !this.team()?.confirmedPlan; }
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
        const command = this.pending() ?? { commandId: commandId(), phase: team.phase, revision: this.householdTeam ? team.commandRevision : team.revision, cars: this.choices().map(car => ({ ...car, pit: car.pit === 'Pit' ? 'Pit' : 'Stay Out', compound: car.pit === 'Pit' ? car.compound : null })) };
        this.pending.set(command);
        this.busy.set(true);
        this.error.set('');
        const generation=this.privateGeneration;
        const submittedTeam=this.householdTeam;
        try {
            const ack = await this.request<Acknowledgement>(this.householdTeam ? '/api/lobbies/'+this.id+'/teams/'+this.householdTeam+'/plans?deviceId='+encodeURIComponent(this.householdDevice) : '/api/sessions/' + this.id + '/plans', command, this.householdTeam ? await this.householdUser!.getIdToken() : this.capability);
            if(generation!==this.privateGeneration || submittedTeam!==this.householdTeam)return;
            if (this.team()?.phase !== command.phase) return;
            if(this.sharedTablet && ack.sessionId===this.id && ack.commandId===command.commandId){await this.passTablet(false);return;}
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
            if(generation!==this.privateGeneration || submittedTeam!==this.householdTeam)return;
            if (this.team()?.phase !== command.phase) return;
            if (this.team()?.confirmedPlan?.commandId === command.commandId)
                return;
            if (error instanceof RequestFailure && error.status >= 400 && error.status < 500) {
                this.pending.set(null);
                this.fail(error);
                if(error.status===403)await this.clearAuthority();
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
            if(generation===this.privateGeneration && this.team()?.phase===command.phase)this.busy.set(false);
        }
    }
    private rememberCreated(value: Created) {
        const id = value.displayUrl.split('/').at(-1)!;
        localStorage.setItem('race-controller:' + id, value.controllerUrl);
        this.created.set(value);
    }
    private async refreshAssignments() {
        const request=++this.assignmentRequest;
        const response=await fetch('/api/lobbies/'+this.id+'/controllers?deviceId='+encodeURIComponent(this.householdDevice),{cache:'no-store',headers:{Authorization:'Bearer '+await this.householdUser!.getIdToken()}});
        const value=await response.json();if(!response.ok)throw new Error(value.message??'Controller authority unavailable.');
        if(request!==this.assignmentRequest)return;
        const assignments=value as ControllerAssignment[];
        this.sharedTeams.set(this.sharedTablet?assignments.filter(team=>team.control==='SharedTablet'):assignments);
        if(this.householdTeam && !this.sharedTeams().some(team=>team.teamId===this.householdTeam)){await this.clearAuthority();throw new Error('Controller permission removed. This device is no longer authorised for this team. Editing has stopped.');}
        this.selectNextTeam();
    }
    private clearPrivateView(){
        this.privateGeneration++;this.team.set(null);this.choices.set([]);this.pending.set(null);this.acknowledged.set(null);this.busy.set(false);this.activePrivate.set(false);
    }
    private async clearAuthority(){
        const own=this.team();const teamId=this.householdTeam;
        this.clearPrivateView();
        if(this.sharedTablet)this.householdTeam='';
        this.handoffMessage.set('Controller permission removed. Private information and drafts were cleared.');
        if(this.connection&&own)await this.connection.invoke('UnsubscribeHouseholdTeam',this.id,teamId,own.assignmentRevision).catch(()=>undefined);
    }
    private async updateAuthority(){
        const before=this.team();await this.refreshAssignments();
        const current=this.sharedTeams().find(team=>team.teamId===this.householdTeam);
        if(before&&current&&current.assignmentRevision!==before.assignmentRevision){
            await this.clearAuthority();
            if(!this.sharedTablet&&this.connection)await this.rejoin(this.connection);
        }
    }
    private selectNextTeam(){const teams=this.sharedTeams();const readiness=this.publicState()?.readiness;this.nextTeam.set((teams.find(team=>!readiness?.find(item=>item.teamId===team.teamId)?.locked)??teams[0])?.teamId??'');}
    teamName(teamId=this.householdTeam){return this.sharedTeams().find(team=>team.teamId===teamId)?.constructorName??teamId.replace('team-','Team ');}
    teamControl(){return this.sharedTeams().find(team=>team.teamId===this.householdTeam)?.control;}
    async readyForTeam(){
        if(!this.sharedTablet || this.activePrivate() || !this.nextTeam() || this.connectionState()!=='connected')return;
        this.householdTeam=this.nextTeam();this.activePrivate.set(true);this.handoffMessage.set('');
        try{if(this.connection)await this.rejoin(this.connection);}catch(error){await this.passTablet(false);this.connectionFailure(error);}
    }
    async passTablet(discard=true){
        const old=this.team();const oldTeam=this.householdTeam;
        this.clearPrivateView();this.householdTeam='';this.error.set('');
        this.handoffMessage.set(discard?'Unsubmitted drafts were discarded.':'Team plan locked. Pass the tablet to the next player.');this.selectNextTeam();
        if(this.connection && oldTeam && old){try{await this.connection.invoke('UnsubscribeHouseholdTeam',this.id,oldTeam,old.assignmentRevision);}catch{await this.connection.stop();this.connectionState.set('disconnected');}}
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
    dispose() { clearInterval(this.countdown); const connection = this.connection; this.connection = undefined; void connection?.stop(); }
}
