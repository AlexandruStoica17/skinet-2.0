export class ShopParams {
    brandId = 0;
    typeId = 0;
    sort = 'name';
    pageNumber = 1;
    pageSize = 6;
    search = '';

    // Price range filter
    minPrice = 0;
    maxPrice = 0;

    // UPDATED: multiselect filters — arrays instead of single string
    skinTypes: string[]  = [];
    usages: string[]     = [];
    benefits: string[]   = [];
    formulas: string[]   = [];

    // Minimum rating filter (0 = no filter)
    minRating = 0;
}