import { Component, effect, signal } from '@angular/core';
import {raceDriverCard} from './race-driver-card';
import { commandId } from './command-id';
import { RaceSession, PublicState } from './race-session';
import { PublicClassification } from '../ui/timing/public-classification.model';
import { TwoCarController } from '../ui/controller/two-car-controller';
import { PrivateControllerViewModel, ControllerState, TeamPlan } from '../ui/controller/controller.model';
import { RaceEvent } from '../ui/results/race-event';
import { PhaseResult } from '../ui/results/phase-result';
import { PublicPhaseResult, PublicRaceEvent } from '../ui/results/public-phase-result.model';
@Component({ selector: 'app-race-page', imports: [TwoCarController, PhaseResult, RaceEvent], templateUrl: './race-page.html', styleUrl: './race-page.css' })
export class RacePage extends RaceSession {
    readonly gapReference = signal<'leader' | 'ahead'>('ahead');
    readonly playbackSeconds = signal(3);
    readonly playing = signal(false);
    readonly revealingOutcome = signal(false);
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
                    if (this.lastSettled) {this.revealingOutcome.set(this.mode==='controller'); this.playFeedback('initial');} else {this.beginSample('recovered');this.skipPlayback('recovered');}
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
    lifecycleLabel(value: string | undefined) { return value === 'awaiting-decisions' ? 'Awaiting decisions' : value === 'resolving' ? 'Resolving play…' : value === 'phase-result' ? 'Resolving play…' : value === 'finished' ? 'Finish confirmed' : 'Loading race'; }
    setPlaybackSeconds(value: string) { const seconds = Number(value); this.playbackSeconds.set(Number.isFinite(seconds) ? Math.min(10, Math.max(0, seconds)) : 3); }
    playFeedback(kind = 'replay') {
        this.finishSample('new-window');
        this.clearPlayback();
        const state = this.currentResult;
        if (!state) return;
        this.beginSample(kind);
        this.presentedState.set(this.mode==='controller' ? state : this.beforeResult ?? state); this.shownEvents.set(0);
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
        this.revealingOutcome.set(false);
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
        const summary = state.presentation?.summary.replace(/phase/gi,'race window') ?? 'Fixed rolling order · 22 synthetic drivers · 11 teams.';
        return { state: this.playing() ? 'presenting' : state.lifecycle === 'finished' ? 'finished' : state.lifecycle === 'resolving' ? 'resolving' : state.presentation ? 'phase-results' : state.phase > 0 ? 'awaiting' : 'initial',
            classification: this.classification(), events: state.events.slice(0, this.shownEvents()).map(event => this.eventModel(event)),
            context: { label: state.lifecycle === 'finished' && !this.playing() ? 'First across the line' : 'Race facts', name: state.lifecycle === 'finished' && !this.playing() ? state.classification[0].name : 'Dry race laboratory', description: summary, imageUrl: null } };
    }
    private eventModel(event: PublicState['events'][number]): PublicRaceEvent {
        const name = 'Driver ' + Number(event.driverId.slice(-2));
        const other = event.otherDriverId ? 'Driver ' + Number(event.otherDriverId.slice(-2)) : '';
        const kind = event.kind === 'Overtake' ? 'overtake' : event.kind === 'PitEntry' ? 'pit-entry' : event.kind === 'PitExit' ? 'pit-exit' : 'tyre-change';
        const summary = kind === 'overtake' ? name + ' passed ' + other + ' on track.' : kind === 'pit-entry' ? name + ' entered the pit lane.' : kind === 'pit-exit' ? name + ' left the pit lane.' : name + ' fitted ' + event.compound + ' tyres.';
        const involvesPlayer = [event.driverId, event.otherDriverId].some(id => this.publicState()?.classification.some(car => car.driverId === id && car.teamId === 'team-01'));
        return { id: event.id, kind, lap: event.lap, driverName: name, portraitUrl: null, compound: event.compound, summary, minimalist: !involvesPlayer };
    }
    controller(): PrivateControllerViewModel {
        const own = this.team()!;
        let state: ControllerState = this.editable() ? (this.error() ? 'rejected' : 'selected') : own.lifecycle === 'resolving' ? 'resolving' : 'locked';
        if (this.connectionState() !== 'connected' || own.lifecycle !== 'resolving' && (this.busy() || this.pending()))
            state = 'loading';
        const message = this.error() || (state === 'selected' ? (own.phase>0?'Next decisions are open. Choose for both cars and approve your plan.':'Choose for both cars and approve your plan.') : state === 'resolving' ? 'Both cars accepted. Resolving play…' : state === 'loading' ? 'Instructions unavailable until current state and submission confirmation are recovered.' : own.lifecycle === 'finished' ? 'Race finished. These are your confirmed final instructions.' : 'Waiting for other teams…');
        return { teamName: this.teamName(own.teamId), submitLabel: 'Confirm plan', phase: own.phase + 1, decisionLaps: this.publicState()?.nextDecisionLaps, state, message, confirmedRevision: own.confirmedPlan ? own.revision : undefined, cars: own.cars.map((car, index) => { const nearbyClassification=this.nearbyClassification(car.driverId); return ({ id: car.driverId, card:raceDriverCard(car.driverId),nearbyClassification,nearbyDrivers:nearbyClassification.rows.map(row=>raceDriverCard(row.driver.id)),recentLaps:this.publicState()?.classification.find(row=>row.driverId===car.driverId)?.recentLapTimes.slice().sort((a,b)=>b.lap-a.lap), name: 'Driver ' + Number(car.driverId.slice(-2)), currentCompound: car.compound, tyreAge: car.tyreAge, tyreEstimate: (car.estimatedTyreCondition * 100).toFixed(0) + '%', battleOptions: car.battleOptions, battleExplanation: car.battleOptions.length > 1 ? '' : 'No current battle opportunity. Neutral remains selected.', pendingStop: car.inPits ? 'Pit stop ongoing. Further pit instructions are unavailable.' : car.stopQueued ? 'Pit stop queued. Further pit instructions are unavailable.' : null, selected: { pace: this.choices()[index].pace, stop: this.choices()[index].pit === 'Pit' ? 'Pit' : 'Stay Out', compound: this.compound(this.choices()[index].compound), battle: this.choices()[index].battle } }); }) };
    }
    private compound(value: string) { if (value === 'Soft' || value === 'Medium' || value === 'Hard')
        return value; throw new Error('Unsupported authoritative compound'); }
    override editable(){return !this.revealingOutcome() && super.editable();}
    submitPlan(plan: TeamPlan) { if (!this.editable())
        return; this.choices.set(plan.cars.map(car => ({ driverId: car.id, pace: car.instruction.pace, pit: car.instruction.stop === 'Pit' ? 'Pit' : 'StayOut', compound: car.instruction.compound, battle: car.instruction.battle }))); void this.submit(); }
    nearbyClassification(id:string): PublicClassification { const classification=this.classification('ahead',this.publicState()!);const index=classification.rows.findIndex(row=>row.driver.id===id);return {...classification,rows:index<0?[]:classification.rows.slice(Math.max(0,index-2),index+3)}; }
    classification(reference=this.gapReference(), state=this.presentedState() ?? this.publicState()!): PublicClassification { return { phase: (state.presentation?.resolvedPhase ?? state.phase) + 1, leaderLap: state.leaderLap, totalLaps: state.totalLaps, gapReference: reference, compact: true, rows: state.classification.map(car => ({ position: car.position, driver: { id: car.driverId, code: car.driverId.slice(-2), name: 'Driver '+Number(car.driverId.slice(-2)), team: { name: car.teamId, accent: '#ff685b' } }, gap: reference === 'leader' ? car.gapToLeader : car.gapToAhead, compound: car.compound, tyreAge: car.tyreAge, completedLaps: car.completedLaps, eligibleAtFinish: car.eligibleAtFinish, completedPitStops:car.completedPitStops,isFastestLap:car.isFastestLap, stopStatus: car.pitState === 'in-pit-lane' ? 'in-pit-lane' : 'on-track' })) }; }
}
