export interface ProductReview {
    id: number;
    buyerName: string;
    rating: number;
    comment: string;
    createdAt: Date;
}

export interface ProducerReview {
    id: number;
    buyerEmail: string;
    rating: number;
    comment: string;
    createdAt: Date;
}

export interface ProducerReviewsResponse {
    reviews: ProducerReview[];
    averageRating: number;
    totalReviews: number;
}