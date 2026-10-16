import { Pipe, PipeTransform } from '@angular/core';
import { apiUrl } from './recipe-api.service';

// Turns the relative imageUrl from the API into an absolute URL under the app's base path.
@Pipe({ name: 'recipeImage', standalone: true })
export class RecipeImagePipe implements PipeTransform {
  transform(url: string | null | undefined): string | null {
    return url ? `${apiUrl}${url}` : null;
  }
}
