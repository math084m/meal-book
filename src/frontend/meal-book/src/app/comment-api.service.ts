import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from './recipe-api.service';

export interface RecipeComment {
  id: string;
  parentId: string | null;
  authorName: string | null;
  body: string;
  createdAt: string;
  updatedAt: string | null;
  isDeleted: boolean;
  canEdit: boolean;
  canDelete: boolean;
  replies: RecipeComment[];
}

export const commentMaxLength = 2000;

@Injectable({ providedIn: 'root' })
export class CommentApiService {
  private readonly http = inject(HttpClient);

  list(recipeId: string): Observable<RecipeComment[]> {
    return this.http.get<RecipeComment[]>(`${apiUrl}recipes/${recipeId}/comments`);
  }

  add(recipeId: string, body: string, parentId: string | null = null): Observable<RecipeComment> {
    return this.http.post<RecipeComment>(`${apiUrl}recipes/${recipeId}/comments`, { body, parentId });
  }

  update(commentId: string, body: string): Observable<RecipeComment> {
    return this.http.put<RecipeComment>(`${apiUrl}comments/${commentId}`, { body });
  }

  delete(commentId: string): Observable<void> {
    return this.http.delete<void>(`${apiUrl}comments/${commentId}`);
  }
}
