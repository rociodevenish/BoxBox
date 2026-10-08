import { Component, signal } from '@angular/core';
import { RaceSession } from './race-session';
import { TimingTower } from '../ui/timing/timing-tower';
import { PublicClassification } from '../ui/timing/public-classification.model';
import { TwoCarController } from '../ui/controller/two-car-controller';
import { PrivateControllerViewModel, ControllerState, TeamPlan } from '../ui/controller/controller.model';
@Component({ selector: 'app-race-page', imports: [TimingTower, TwoCarController], templateUrl: './race-page.html', styleUrl: './race-page.css' })
export class RacePage extends RaceSession {
    readonly gapReference = signal<'leader' | 'ahead'>('leader');
    constructor() { super(new URL(location.href)); }
    ngOnDestroy() { this.dispose(); }
    lifecycleLabel(value: string | undefined) { return value === 'awaiting-decisions' ? 'Awaiting team plan' : value === 'resolving' ? 'Resolving race phase' : value === 'phase-result' ? 'Phase complete' : value === 'finished' ? 'Race finished' : 'Loading race'; }
    controller(): PrivateControllerViewModel {
        const own = this.team()!;
        let state: ControllerState = this.editable() ? (this.error() ? 'rejected' : 'selected') : own.lifecycle === 'resolving' ? 'resolving' : 'locked';
        if (this.connectionState() !== 'connected' || own.lifecycle !== 'resolving' && (this.busy() || this.pending()))
            state = 'loading';
        const message = this.error() || (state === 'selected' ? 'Draft choices are editable. Nothing has been submitted.' : state === 'resolving' ? 'Both cars accepted. Race phase resolving.' : state === 'loading' ? 'Instructions unavailable until current state and submission confirmation are recovered.' : 'Both cars locked. These are your confirmed instructions for this phase.');
        return { teamName: 'Team 01', phase: own.phase + 1, state, message, confirmedRevision: own.confirmedPlan ? own.revision : undefined, cars: own.cars.map((car, index) => ({ id: car.driverId, name: 'Driver ' + car.driverId.slice(-2), currentCompound: car.compound, tyreAge: car.tyreAge, tyreEstimate: (car.estimatedTyreCondition * 100).toFixed(0) + '%', battleOptions: car.battleOptions, battleExplanation: car.battleOptions.length > 1 ? 'Only battle choices available for the current nearby encounter are offered.' : 'No current battle opportunity. Neutral remains selected.', pendingStop: car.inPits ? 'Pit stop ongoing. Further pit instructions are unavailable.' : car.stopQueued ? 'Pit stop queued. Further pit instructions are unavailable.' : null, selected: { pace: this.choices()[index].pace, stop: this.choices()[index].pit === 'Pit' ? 'Pit' : 'Stay Out', compound: this.compound(this.choices()[index].compound), battle: this.choices()[index].battle } })) };
    }
    private compound(value: string) { if (value === 'Soft' || value === 'Medium' || value === 'Hard')
        return value; throw new Error('Unsupported authoritative compound'); }
    submitPlan(plan: TeamPlan) { if (!this.editable())
        return; this.choices.set(plan.cars.map(car => ({ driverId: car.id, pace: car.instruction.pace, pit: car.instruction.stop === 'Pit' ? 'Pit' : 'StayOut', compound: car.instruction.compound, battle: car.instruction.battle }))); void this.submit(); }
    classification(): PublicClassification { const state = this.publicState()!; return { phase: state.phase + 1, leaderLap: state.leaderLap, totalLaps: state.totalLaps, gapReference: this.gapReference(), rows: state.classification.map(car => ({ position: car.position, driver: { id: car.driverId, code: car.driverId.slice(-2), name: car.name, team: { name: car.teamId, accent: '#ff685b' } }, gap: this.gapReference() === 'leader' ? car.gapToLeader : car.gapToAhead, compound: car.compound, tyreAge: car.tyreAge, stopStatus: car.pitState === 'in-pit-lane' ? 'in-pit-lane' : 'on-track' })) }; }
}
