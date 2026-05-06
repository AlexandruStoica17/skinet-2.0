export interface User {
  email: string;
  displayName: string;
  token: string;
  role?: string; // Adăugat
  isVerified?: boolean; // Adăugat
}

export interface Address {
  firstName: string;
  lastName: string;
  street: string;
  city: string;
  state: string;
  zipcode: string;
}

export interface RegisterValues {
  displayName: string;
  email: string;
  password: string;
  role: string; // Nou
  companyName?: string; // Nou (opțional)
}
