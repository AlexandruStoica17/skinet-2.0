import { Component, EventEmitter, Input, Output, OnInit } from '@angular/core';
import { BasketItem } from '../models/basket';
import { BasketService } from 'src/app/basket/basket.service';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-basket-summary',
  templateUrl: './basket-summary.component.html',
  styleUrls: ['./basket-summary.component.scss'],
})
export class BasketSummaryComponent implements OnInit {

  @Output() addItem = new EventEmitter<BasketItem>();
  @Output() removeItem = new EventEmitter<{id: number, quantity: number}>();
  @Input() isBasket = true;

  // Variabila care va ține coșul grupat pe magazine
  groupedBasket$!: Observable<{producerName: string, items: BasketItem[]}[]>;

  constructor(public basketService: BasketService) {}

  ngOnInit() {
    // Aici facem magia: interceptăm coșul și îl grupăm
    this.groupedBasket$ = this.basketService.basketSource$.pipe(
      map(basket => {
        if (!basket) return [];
        
        const grouped = basket.items.reduce((acc, item) => {
          const producer = item.producerName || 'Our Store'; // Fallback
          if (!acc[producer]) {
            acc[producer] = []; // Creăm o "cutie" nouă pentru acest magazin
          }
          acc[producer].push(item); // Punem produsul în cutia lui
          return acc;
        }, {} as { [key: string]: BasketItem[] });

        // Transformăm obiectul într-un array ca să meargă *ngFor în HTML
        return Object.keys(grouped).map(key => ({
          producerName: key,
          items: grouped[key]
        }));
      })
    );
  }

  addBasketItem(item: BasketItem){
    this.addItem.emit(item);
  }

  removeBasketItem(id: number, quantity = 1){
    this.removeItem.emit({id, quantity});
  }
}
