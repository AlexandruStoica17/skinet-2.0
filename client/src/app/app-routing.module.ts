import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { TestErrorComponent } from './core/test-error/test-error.component';
import { NotFoundComponent } from './core/not-found/not-found.component';
import { ServerErrorComponent } from './core/server-error/server-error.component';
import { AuthGuard } from './core/guards/auth.guard';
import { AdminGuard } from './core/guards/admin.guard';
import { AdminComponent } from './admin/admin.component';
import { AddProductComponent } from './producer/add-product/add-product.component';
import { MyProductsComponent } from './producer/my-products/my-products.component';
import { EditProductComponent } from './producer/edit-product/edit-product.component';
import { ProducerGuard } from './core/guards/producer.guard';
import { ProducerOrdersComponent } from './producer/producer-orders/producer-orders.component';


const routes: Routes = [
  { path: '', component: HomeComponent, data: { breadcrumb: 'Home' } },
  {
    path: 'test-error', 
    component: TestErrorComponent,
    data: { breadcrumb: 'Test Errors' },
  },
  {
    path: 'server-error',
    component: ServerErrorComponent,
    data: { breadcrumb: 'Server Error' },
  },
  {
    path: 'not-found',
    component: NotFoundComponent,
    data: { breadcrumb: 'Not Found' },
  },
  {
    path: 'shop',
    loadChildren: () =>
      import('./shop/shop.module').then((mod) => mod.ShopModule),
  },
  {
    path: 'basket',
    loadChildren: () =>
      import('./basket/basket.module').then((mod) => mod.BasketModule),
  },
  {
    path: 'checkout',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./checkout/checkout.module').then((mod) => mod.CheckoutModule),
  },
  {
    path: 'orders',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./orders/orders.module').then((mod) => mod.OrdersModule),
    data: { breadcrumb: 'Orders' },
  },
  {
    path: 'account',
    loadChildren: () =>
      import('./account/account.module').then((mod) => mod.AccountModule),
    data: { breadcrumb: { skip: true } },
  },
  { 
    path: 'chat', 
    loadChildren: () => import('./chat/chat.module').then(m => m.ChatModule) 
  },
  { path: 'favorites', loadChildren: () => import('./favorites/favorites.module').then(m => m.FavoritesModule) },
  { path: 'blog', loadChildren: () => import('./blog/blog.module').then(m => m.BlogModule) },

  // --- RUTELE NOASTRE DE ADMIN / PRODUCĂTOR ---
  { path: 'admin', component: AdminComponent, canActivate: [AdminGuard] },
  // { path: 'add-product', component: AddProductComponent },
  // { path: 'my-products', component: MyProductsComponent },
  // { path: 'edit-product/:id', component: EditProductComponent },

  // { path: 'producer/orders', component: ProducerOrdersComponent, data: { breadcrumb: 'Producer Orders' } },

  // Rute protejate DOAR pentru producători
  { path: 'add-product', component: AddProductComponent, canActivate: [ProducerGuard] },
  { path: 'my-products', component: MyProductsComponent, canActivate: [ProducerGuard] },
  { path: 'edit-product/:id', component: EditProductComponent, canActivate: [ProducerGuard] },
  // Adaugă asta lângă celelalte rute de producător:
  { path: 'producer-orders', component: ProducerOrdersComponent, data: { breadcrumb: 'Comenzi Vânzător' } },
  {
  path: 'whats-new',
  loadChildren: () => import('./news/news.module').then(m => m.NewsModule),
  data: { breadcrumb: "What's New" }
},

  // --- RUTA WILDCARD TREBUIE SĂ FIE STRICT ULTIMA! ---
  { path: '**', redirectTo: 'not-found', pathMatch: 'full' }

  
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}