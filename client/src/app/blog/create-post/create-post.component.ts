import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { take } from 'rxjs';
import { AccountService } from 'src/app/account/account.service';
import { BlogService } from 'src/app/core/services/blog.service';

interface BlogSectionForm {
  heading: string;
  text: string;
  caption: string;
  image: File | null;
  imagePreview: string | ArrayBuffer | null;
}

@Component({
  selector: 'app-create-post',
  templateUrl: './create-post.component.html',
  styleUrls: ['./create-post.component.scss']
})
export class CreatePostComponent implements OnInit {
  blogForm!: FormGroup;
  isBlogger = false;
  submitting = false;

  coverImage: File | null = null;
  coverPreview: string | ArrayBuffer | null = null;

  sections: BlogSectionForm[] = [
    {
      heading: '',
      text: '',
      caption: '',
      image: null,
      imagePreview: null
    }
  ];

  constructor(
    private fb: FormBuilder,
    private blogService: BlogService,
    private accountService: AccountService,
    private toastr: ToastrService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.createForm();

    this.accountService.currentUser$.pipe(take(1)).subscribe(user => {
      const role = user?.role;

      if (Array.isArray(role)) {
        this.isBlogger = role.includes('Blogger');
      } else {
        this.isBlogger = role === 'Blogger';
      }

      if (!this.isBlogger) {
        this.toastr.error('Only users with the Blogger role can create articles.');
        this.router.navigateByUrl('/blog');
      }
    });
  }

  createForm() {
    this.blogForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(5)]]
    });
  }

  onCoverSelected(event: any) {
    const file = event.target.files[0];

    if (!file) return;

    this.coverImage = file;

    const reader = new FileReader();

    reader.onload = () => {
      this.coverPreview = reader.result;
    };

    reader.readAsDataURL(file);
  }

  removeCover() {
    this.coverImage = null;
    this.coverPreview = null;
  }

  addSection() {
    this.sections.push({
      heading: '',
      text: '',
      caption: '',
      image: null,
      imagePreview: null
    });
  }

  removeSection(index: number) {
    if (this.sections.length === 1) {
      this.toastr.info('The article must have at least one section.');
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

    const reader = new FileReader();

    reader.onload = () => {
      this.sections[index].imagePreview = reader.result;
    };

    reader.readAsDataURL(file);
  }

  removeSectionImage(index: number) {
    this.sections[index].image = null;
    this.sections[index].imagePreview = null;
  }

  isArticleValid(): boolean {
    if (this.blogForm.invalid) return false;

    return this.sections.some(section =>
      section.heading.trim().length > 0 ||
      section.text.trim().length > 0 ||
      section.image !== null
    );
  }

  onSubmit() {
    if (!this.isArticleValid()) {
      this.blogForm.markAllAsTouched();
      this.toastr.error('Add a title and at least one section with text or an image.');
      return;
    }

    this.submitting = true;

    const formData = new FormData();

    formData.append('title', this.blogForm.get('title')?.value);

    if (this.coverImage) {
      formData.append('image', this.coverImage);
    }

    const sectionsPayload = this.sections.map((section, index) => ({
      heading: section.heading,
      text: section.text,
      caption: section.caption,
      displayOrder: index + 1
    }));

    formData.append('sectionsJson', JSON.stringify(sectionsPayload));

    // Content simplificat pentru compatibilitate cu search/sugestii
    const plainContent = this.sections
      .map(section => `${section.heading}\n${section.text}`.trim())
      .filter(x => x.length > 0)
      .join('\n\n');

    formData.append('content', plainContent);

    // Fiecare imagine de secțiune primește nume unic: sectionImages_0, sectionImages_1...
    this.sections.forEach((section, index) => {
      if (section.image) {
        formData.append(`sectionImages_${index}`, section.image);
      }
    });

    this.blogService.createPost(formData).subscribe({
      next: (post: any) => {
        this.toastr.success('Article published successfully.');

        if (post?.id) {
          this.router.navigate(['/blog', post.id]);
        } else {
          this.router.navigateByUrl('/blog');
        }
      },
      error: error => {
        console.log(error);
        this.submitting = false;
        this.toastr.error('Could not publish the article.');
      }
    });
  }

  trackByIndex(index: number) {
    return index;
  }
}
