import { Component } from '@angular/core';
import { BasketService } from './basket.service';
import { BasketItem } from '../shared/models/basket';

@Component({
  selector: 'app-basket',
  templateUrl: './basket.component.html',
  styleUrls: ['./basket.component.scss']
})
export class BasketComponent {

  constructor(public basketService: BasketService) {}

  incrementQuantity(item: BasketItem){
    this.basketService.addItemToBasket(item);
  }

  removeItem(event: {id: number, quantity: number}){
    this.basketService.removeItemFromBasket(event.id, event.quantity);
  }

  // Adaugă această funcție în clasa BasketComponent
  getGroupedItems(items: any[]) {
    const grouped = items.reduce((acc, item) => {
      const producer = item.producerName || 'Magazinul Nostru'; // Dacă nu are nume, îl punem la comun
      if (!acc[producer]) {
        acc[producer] = [];
      }
      acc[producer].push(item);
      return acc;
    }, {} as { [key: string]: any[] });

    // Transformăm dicționarul într-un array ușor de parcurs în HTML
    return Object.keys(grouped).map(key => ({
      producerName: key,
      items: grouped[key]
    }));
  }

}
