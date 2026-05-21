export interface PostSection {
  id: number;
  heading: string;
  text: string;
  imageUrl?: string;
  caption?: string;
  displayOrder: number;
}

export interface Post {
  id: number;
  title: string;

  // Păstrat pentru articole vechi/search
  summary?: string;
  content: string;

  // Cover image
  imageUrl?: string;

  authorName: string;
  createdAt: string;

  // NOU: secțiuni cu imagini multiple
  sections?: PostSection[];
}