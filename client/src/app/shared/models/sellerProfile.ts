import { Pagination } from './pagination';
import { Product } from './product';

export interface SellerProfile {
  email: string;
  displayName: string;
  companyName: string;
  isVerified: boolean;
  documentUrl?: string;
  description?: string;
  story?: string;
  history?: string;
  location?: string;
  mapUrl?: string;
  sellerType: string;
  products: Pagination<Product[]>;
}

export interface SellerProfileUpdate {
  companyName: string;
  description: string;
  story: string;
  history: string;
  location: string;
  mapUrl: string;
}
