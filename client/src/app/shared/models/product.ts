
export interface ProductPhoto {
  id: number;
  url: string;
  isMain: boolean;
  displayOrder: number;
}

export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;

  // Păstrăm poza principală existentă ca să nu stricăm cardurile/basket-ul.
  pictureUrl: string;

  // MODIFICAT: poze multiple pentru pagina de produs / galerie
  photos?: ProductPhoto[];

  productType: string;
  productBrand: string;
  productTypeId: number;
  productBrandId: number;

  producerId: string;
  producerName: string;
  producerEmail: string;

  // MODIFICAT: informații afișate în pagina produsului
  skinType?: string;
  usage?: string;
  benefits?: string;
  formula?: string;

  // Opțional, util pentru What's New dacă îl adaugi și în backend
  createdAt?: string;
}