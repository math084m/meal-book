import { HttpClient } from '@angular/common/http';
import { Injectable, OnDestroy, Pipe, PipeTransform, inject } from '@angular/core';
import { AuthService } from './auth.service';
import { apiUrl } from './recipe-api.service';

// Admins can see unpublished recipes, whose images need the bearer token. An <img> tag cannot send
// it, so for admins the image is fetched through HttpClient and shown as a blob URL.
@Injectable({ providedIn: 'root' })
export class AuthImageCache implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly blobs = new Map<string, string | null>();

  get(url: string): string | null | undefined {
    if (!this.blobs.has(url)) {
      this.blobs.set(url, null);
      this.http.get(url, { responseType: 'blob' }).subscribe({
        next: blob => this.blobs.set(url, URL.createObjectURL(blob)),
        error: () => this.blobs.set(url, null)
      });
    }
    return this.blobs.get(url);
  }

  ngOnDestroy(): void {
    this.blobs.forEach(value => value && URL.revokeObjectURL(value));
  }
}

// Turns the relative imageUrl from the API into a URL the <img> can load.
@Pipe({ name: 'recipeImage', standalone: true, pure: false })
export class RecipeImagePipe implements PipeTransform {
  private readonly auth = inject(AuthService);
  private readonly cache = inject(AuthImageCache);

  transform(url: string | null | undefined): string | null {
    if (!url) return null;
    const absolute = `${apiUrl}${url}`;
    return this.auth.currentUser()?.isAdmin ? (this.cache.get(absolute) ?? null) : absolute;
  }
}
