import { Component, effect, signal } from '@angular/core';
import { commandId } from './command-id';
import { RaceSession, PublicState } from './race-session';
import { PublicClassification } from '../ui/timing/public-classification.model';
import { TwoCarController } from '../ui/controller/two-car-controller';
import { PrivateControllerViewModel, ControllerState, TeamPlan } from '../ui/controller/controller.model';
import { PhaseResult } from '../ui/results/phase-result';
import { PublicPhaseResult, PublicRaceEvent } from '../ui/results/public-phase-result.model';
@Component({ selector: 'app-race-page', imports: [TwoCarController, PhaseResult], templateUrl: './race-page.html', styleUrl: './race-page.css' })
export class RacePage extends RaceSession {
    readonly gapReference = signal<'leader' | 'ahead'>('leader');
    readonly playbackSeconds = signal(3);
    readonly playing = signal(false);
    readonly shownEvents = signal(0);
    readonly presentedState = signal<PublicState | null>(null);
    private playbackTimer?: ReturnType<typeof setInterval>;
    private cursor?: string;
    private lastSettled?: PublicState;
    private beforeResult?: PublicState;
    private currentResult?: PublicState;
    private readonly pageStarted = performance.now();
    private readonly presentationDevice = commandId();
    private presentationSample?: {sampleId:string;cursor:string;started:number;playbackKind:string};
    constructor() {
        super(new URL(location.href));
        effect(() => {
            const state = this.publicState();
            if (!state) return;
            if (state.presentation) {
                this.currentResult = state;
                if (this.cursor !== state.presentation.cursor) {
                    this.cursor = state.presentation.cursor;
                    this.beforeResult = this.lastSettled ?? state;
                    if (this.lastSettled) this.playFeedback('initial'); else {this.beginSample('recovered');this.skipPlayback('recovered');}
                }
            } else {
                this.finishSample('new-window');
                this.clearPlayback(); this.cursor = undefined;
                this.playing.set(false); this.shownEvents.set(0);
                this.presentedState.set(state); this.lastSettled = state;
            }
        });
    }
    ngOnDestroy() { this.clearPlayback(); this.dispose(); }
    lifecycleLabel(value: string | undefined) { return value === 'awaiting-decisions' ? 'Awaiting team plan' : value === 'resolving' ? 'Resolving race phase' : value === 'phase-result' ? 'Presenting phase result' : value === 'finished' ? 'Race finished' : 'Loading race'; }
    setPlaybackSeconds(value: string) { const seconds = Number(value); this.playbackSeconds.set(Number.isFinite(seconds) ? Math.min(10, Math.max(0, seconds)) : 3); }
    playFeedback(kind = 'replay') {
        this.finishSample('new-window');
        this.clearPlayback();
        const state = this.currentResult;
        if (!state) return;
        this.beginSample(kind);
        this.presentedState.set(this.beforeResult ?? state); this.shownEvents.set(0);
        const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches;
        const seconds = reduced ? 0 : this.playbackSeconds();
        if (seconds === 0) { this.skipPlayback(reduced ? 'reduced-motion' : 'completed'); return; }
        this.playing.set(true);
        this.playbackTimer = setInterval(() => {
            const count = this.shownEvents() + 1;
            this.shownEvents.set(Math.min(count, state.events.length));
            if (count >= state.events.length) this.skipPlayback('completed');
        }, seconds * 1000 / Math.max(1, state.events.length));
    }
    skipPlayback(completion = 'skipped') {
        this.finishSample(completion);
        this.clearPlayback(); this.playing.set(false);
        if (this.currentResult) {
            this.shownEvents.set(this.currentResult.events.length);
            this.presentedState.set(this.currentResult); this.lastSettled = this.currentResult;
        }
    }
    private beginSample(playbackKind:string) {
        if(this.mode==='display' && this.currentResult?.presentation) this.presentationSample={sampleId:commandId(),cursor:this.currentResult.presentation.cursor,started:performance.now(),playbackKind};
    }
    private finishSample(completion:string) {
        const sample=this.presentationSample;this.presentationSample=undefined;
        if(!sample)return;
        // Advisory client diagnostics never gate playback, submission or authoritative state.
        void fetch('/api/sessions/'+this.id+'/presentation-timing',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({sampleId:sample.sampleId,deviceId:this.presentationDevice,cursor:sample.cursor,durationMilliseconds:performance.now()-sample.started,clientElapsedMilliseconds:performance.now()-this.pageStarted,playbackKind:sample.playbackKind,completion:completion==='recovered'?'completed':completion})}).catch(()=>undefined);
    }
    private clearPlayback() { if (this.playbackTimer) clearInterval(this.playbackTimer); this.playbackTimer = undefined; }
    phaseResult(): PublicPhaseResult {
        const state = this.publicState()!;
        const summary = state.presentation?.summary ?? 'Fixed rolling order · 22 synthetic drivers · 11 teams.';
        return { state: this.playing() ? 'presenting' : state.lifecycle === 'finished' ? 'finished' : state.lifecycle === 'resolving' ? 'resolving' : state.presentation ? 'phase-results' : state.phase > 0 ? 'awaiting' : 'initial',
            classification: this.classification(), events: state.events.slice(0, this.shownEvents()).map(event => this.eventModel(event)),
            context: { label: state.lifecycle === 'finished' && !this.playing() ? 'First across the line' : 'Phase facts', name: state.lifecycle === 'finished' && !this.playing() ? state.classification[0].name : 'Dry race laboratory', description: summary, imageUrl: null } };
    }
    private eventModel(event: PublicState['events'][number]): PublicRaceEvent {
        const name = 'Driver ' + event.driverId.slice(-2);
        const other = event.otherDriverId ? 'Driver ' + event.otherDriverId.slice(-2) : '';
        const kind = event.kind === 'Overtake' ? 'overtake' : event.kind === 'PitEntry' ? 'pit-entry' : event.kind === 'PitExit' ? 'pit-exit' : 'tyre-change';
        const summary = kind === 'overtake' ? name + ' passed ' + other + ' on track.' : kind === 'pit-entry' ? name + ' entered the pit lane.' : kind === 'pit-exit' ? name + ' left the pit lane.' : name + ' fitted ' + event.compound + ' tyres.';
        return { id: event.id, kind, lap: event.lap, driverName: name, portraitUrl: null, compound: event.compound, summary };
    }
    controller(): PrivateControllerViewModel {
        const own = this.team()!;
        let state: ControllerState = this.editable() ? (this.error() ? 'rejected' : 'selected') : own.lifecycle === 'resolving' ? 'resolving' : 'locked';
        if (this.connectionState() !== 'connected' || own.lifecycle !== 'resolving' && (this.busy() || this.pending()))
            state = 'loading';
        const message = this.error() || (state === 'selected' ? (own.phase>0?'Next phase choices are open. Change either car, review both and run when ready.':'Draft choices are editable. Review both cars to run this phase.') : state === 'resolving' ? 'Both cars accepted. Running the phase; the next decision window opens automatically.' : state === 'loading' ? 'Instructions unavailable until current state and submission confirmation are recovered.' : own.lifecycle === 'finished' ? 'Race finished. These are your confirmed final instructions.' : 'Phase accepted. The next decision window opens automatically.');
        return { teamName: 'Team 01', submitLabel: 'Run phase with both cars', phase: own.phase + 1, state, message, confirmedRevision: own.confirmedPlan ? own.revision : undefined, cars: own.cars.map((car, index) => ({ id: car.driverId, name: 'Driver ' + car.driverId.slice(-2), currentCompound: car.compound, tyreAge: car.tyreAge, tyreEstimate: (car.estimatedTyreCondition * 100).toFixed(0) + '%', battleOptions: car.battleOptions, battleExplanation: car.battleOptions.length > 1 ? 'Only battle choices available for the current nearby encounter are offered.' : 'No current battle opportunity. Neutral remains selected.', pendingStop: car.inPits ? 'Pit stop ongoing. Further pit instructions are unavailable.' : car.stopQueued ? 'Pit stop queued. Further pit instructions are unavailable.' : null, selected: { pace: this.choices()[index].pace, stop: this.choices()[index].pit === 'Pit' ? 'Pit' : 'Stay Out', compound: this.compound(this.choices()[index].compound), battle: this.choices()[index].battle } })) };
    }
    private compound(value: string) { if (value === 'Soft' || value === 'Medium' || value === 'Hard')
        return value; throw new Error('Unsupported authoritative compound'); }
    submitPlan(plan: TeamPlan) { if (!this.editable())
        return; this.choices.set(plan.cars.map(car => ({ driverId: car.id, pace: car.instruction.pace, pit: car.instruction.stop === 'Pit' ? 'Pit' : 'StayOut', compound: car.instruction.compound, battle: car.instruction.battle }))); void this.submit(); }
    classification(): PublicClassification { const state = this.presentedState() ?? this.publicState()!; return { phase: (state.presentation?.resolvedPhase ?? state.phase) + 1, leaderLap: state.leaderLap, totalLaps: state.totalLaps, gapReference: this.gapReference(), compact: true, rows: state.classification.map(car => ({ position: car.position, driver: { id: car.driverId, code: car.driverId.slice(-2), name: car.name, team: { name: car.teamId, accent: '#ff685b' } }, gap: this.gapReference() === 'leader' ? car.gapToLeader : car.gapToAhead, compound: car.compound, tyreAge: car.tyreAge, completedLaps: car.completedLaps, eligibleAtFinish: car.eligibleAtFinish, stopStatus: car.pitState === 'in-pit-lane' ? 'in-pit-lane' : 'on-track' })) }; }
}
