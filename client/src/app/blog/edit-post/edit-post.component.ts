import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { BlogService } from 'src/app/core/services/blog.service';
import { Post } from 'src/app/shared/models/post';

interface BlogSectionForm {
  id?: number;
  heading: string;
  text: string;
  caption: string;
  image: File | null;
  imagePreview: string | ArrayBuffer | null;
  existingImageUrl?: string;
  removeImage: boolean;
}

@Component({
  selector: 'app-edit-post',
  templateUrl: './edit-post.component.html',
  styleUrls: ['./edit-post.component.scss']
})
export class EditPostComponent implements OnInit {
  blogForm!: FormGroup;
  postId!: number;
  post?: Post;

  loading = false;
  submitting = false;

  coverImage: File | null = null;
  coverPreview: string | ArrayBuffer | null = null;
  existingCoverUrl?: string;
  removeCoverImage = false;

  sections: BlogSectionForm[] = [];

  constructor(
    private fb: FormBuilder,
    private blogService: BlogService,
    private toastr: ToastrService,
    private router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.createForm();

    this.postId = +this.route.snapshot.paramMap.get('id')!;

    if (this.postId) {
      this.loadPost();
    }
  }

  createForm() {
    this.blogForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(5)]]
    });
  }

  loadPost() {
    this.loading = true;

    this.blogService.getPost(this.postId).subscribe({
      next: post => {
        this.post = post;
        this.loading = false;

        this.blogForm.patchValue({
          title: post.title
        });

        this.existingCoverUrl = post.imageUrl;
        this.removeCoverImage = false;

        if (post.sections && post.sections.length > 0) {
          this.sections = post.sections
            .sort((a, b) => a.displayOrder - b.displayOrder)
            .map(section => ({
              id: section.id,
              heading: section.heading || '',
              text: section.text || '',
              caption: section.caption || '',
              image: null,
              imagePreview: null,
              existingImageUrl: section.imageUrl,
              removeImage: false
            }));
        } else {
          this.sections = [
            {
              heading: '',
              text: post.content || '',
              caption: '',
              image: null,
              imagePreview: null,
              existingImageUrl: undefined,
              removeImage: false
            }
          ];
        }
      },
      error: error => {
        console.log(error);
        this.loading = false;
        this.toastr.error('Nu s-a putut încărca articolul.');
      }
    });
  }

  onCoverSelected(event: any) {
    const file = event.target.files[0];

    if (!file) return;

    this.coverImage = file;
    this.removeCoverImage = false;

    const reader = new FileReader();

    reader.onload = () => {
      this.coverPreview = reader.result;
    };

    reader.readAsDataURL(file);
  }

  removeCover() {
    this.coverImage = null;
    this.coverPreview = null;
    this.existingCoverUrl = undefined;
    this.removeCoverImage = true;
  }

  addSection() {
    this.sections.push({
      heading: '',
      text: '',
      caption: '',
      image: null,
      imagePreview: null,
      removeImage: false
    });
  }

  removeSection(index: number) {
    if (this.sections.length === 1) {
      this.toastr.info('Articolul trebuie să aibă cel puțin o secțiune.');
      return;
    }

    this.sections.splice(index, 1);
  }

  moveSection(index: number, direction: 'up' | 'down') {
    if (direction === 'up' && index === 0) return;
    if (direction === 'down' && index === this.sections.length - 1) return;

    const newIndex = direction === 'up' ? index - 1 : index + 1;
    const current = this.sections[index];

    this.sections.splice(index, 1);
    this.sections.splice(newIndex, 0, current);
  }

  onSectionImageSelected(event: any, index: number) {
    const file = event.target.files[0];

    if (!file) return;

    this.sections[index].image = file;
    this.sections[index].removeImage = false;

    const reader = new FileReader();

    reader.onload = () => {
      this.sections[index].imagePreview = reader.result;
    };

    reader.readAsDataURL(file);
  }

  removeSectionImage(index: number) {
    this.sections[index].image = null;
    this.sections[index].imagePreview = null;
    this.sections[index].existingImageUrl = undefined;
    this.sections[index].removeImage = true;
  }

  isArticleValid(): boolean {
    if (this.blogForm.invalid) return false;

    return this.sections.some(section =>
      section.heading.trim().length > 0 ||
      section.text.trim().length > 0 ||
      section.image !== null ||
      !!section.existingImageUrl
    );
  }

  onSubmit() {
    if (!this.isArticleValid()) {
      this.blogForm.markAllAsTouched();
      this.toastr.error('Adaugă titlu și cel puțin o secțiune.');
      return;
    }

    this.submitting = true;

    const formData = new FormData();

    formData.append('title', this.blogForm.get('title')?.value);

    if (this.coverImage) {
      formData.append('image', this.coverImage);
    }

    formData.append('removeCoverImage', String(this.removeCoverImage));

    const sectionsPayload = this.sections.map((section, index) => ({
      id: section.id,
      heading: section.heading,
      text: section.text,
      caption: section.caption,
      displayOrder: index + 1,
      removeImage: section.removeImage
    }));

    formData.append('sectionsJson', JSON.stringify(sectionsPayload));

    const plainContent = this.sections
      .map(section => `${section.heading}\n${section.text}`.trim())
      .filter(x => x.length > 0)
      .join('\n\n');

    formData.append('content', plainContent);

    this.sections.forEach((section, index) => {
      if (section.image) {
        formData.append(`sectionImages_${index}`, section.image);
      }
    });

    this.blogService.updatePost(this.postId, formData).subscribe({
      next: post => {
        this.toastr.success('Articolul a fost actualizat.');
        this.router.navigate(['/blog', post.id]);
      },
      error: error => {
        console.log(error);
        this.submitting = false;
        this.toastr.error('Nu s-a putut actualiza articolul.');
      }
    });
  }

  trackByIndex(index: number) {
    return index;
  }
}