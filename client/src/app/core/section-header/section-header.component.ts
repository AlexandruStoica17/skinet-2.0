import { Component } from '@angular/core';
import { BreadcrumbService } from 'xng-breadcrumb';
import { Breadcrumb } from 'xng-breadcrumb/lib/types/breadcrumb';

@Component({
  selector: 'app-section-header',
  templateUrl: './section-header.component.html',
  styleUrls: ['./section-header.component.scss']
})
export class SectionHeaderComponent {

  constructor(public bcService: BreadcrumbService) {}

  getCurrentLabel(breadcrumbs: Breadcrumb[]): string {
    return breadcrumbs.length > 0 ? String(breadcrumbs[breadcrumbs.length - 1].label) : '';
  }

  getDisplayLabel(breadcrumbs: Breadcrumb[]): string {
    const label = this.getCurrentLabel(breadcrumbs);
    return label.toLowerCase() === 'shop' ? 'Products' : label;
  }

  isProductsHeader(breadcrumbs: Breadcrumb[]): boolean {
    return this.getCurrentLabel(breadcrumbs).toLowerCase() === 'shop';
  }
  
}
