import {Component,input,signal} from '@angular/core';
import {ContentCardViewModel,imageUrl} from './content-card.model';
@Component({selector:'app-content-card',templateUrl:'./content-card.html',styleUrl:'./content-card.css'})
export class ContentCard {
 readonly card=input.required<ContentCardViewModel>();readonly failed=signal<string|null>(null);readonly url=imageUrl;
}
