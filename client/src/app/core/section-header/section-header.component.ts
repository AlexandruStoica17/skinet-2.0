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
    if (breadcrumbs.length === 0) {
      return '';
    }

    const current = breadcrumbs[breadcrumbs.length - 1] as Breadcrumb & { label?: unknown; text?: unknown };
    return String(current.label ?? current.text ?? '');
  }

  getDisplayLabel(breadcrumbs: Breadcrumb[]): string {
    const label = this.getCurrentLabel(breadcrumbs);
    return label.toLowerCase() === 'shop' ? 'Products' : label;
  }

  isProductsHeader(breadcrumbs: Breadcrumb[]): boolean {
    return this.getCurrentLabel(breadcrumbs).toLowerCase() === 'shop';
  }
  
}
