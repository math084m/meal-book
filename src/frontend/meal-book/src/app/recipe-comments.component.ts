import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, effect, inject, input, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { CommentApiService, RecipeComment, commentMaxLength } from './comment-api.service';
import { I18nService, TranslationKey } from './i18n.service';

// Threads are one level deep: replying to a reply joins the same thread on the server.
@Component({
  selector: 'app-recipe-comments',
  standalone: true,
  imports: [DatePipe, FormsModule, NgTemplateOutlet, RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <section class="recipe-comments" aria-labelledby="comments-title">
      <p class="eyebrow">{{ i18n.t('comments.eyebrow') }}</p>
      <h2 id="comments-title">{{ i18n.t('comments.title') }}</h2>

      @if (auth.currentUser()?.displayName) {
        <form class="comment-form" (ngSubmit)="post()">
          <label>{{ i18n.t('comments.new') }}
            <textarea name="newComment" rows="3" [maxlength]="maxLength" [(ngModel)]="draft" [placeholder]="i18n.t('comments.placeholder')"></textarea>
          </label>
          <button class="button" type="submit" [disabled]="busy() || !draft.trim()">{{ i18n.t('comments.post') }}</button>
        </form>
      } @else {
        <p class="comment-name-prompt">{{ i18n.t('comments.nameRequired') }} <a routerLink="/profile">{{ i18n.t('profile.banner.link') }}</a></p>
      }
      @if (error()) { <p class="error-message" role="alert">{{ i18n.t(error()!) }}</p> }

      @if (threads(); as list) {
        @if (list.length) {
          <ol class="comment-list">
            @for (thread of list; track thread.id) {
              <li class="comment-thread">
                <ng-container *ngTemplateOutlet="commentTpl; context: { $implicit: thread }" />
                @if (thread.replies.length) {
                  <ol class="comment-replies">
                    @for (reply of thread.replies; track reply.id) {
                      <li><ng-container *ngTemplateOutlet="commentTpl; context: { $implicit: reply }" /></li>
                    }
                  </ol>
                }
                @if (replyingTo() && threadOf(replyingTo()!) === thread.id) {
                  <form class="comment-form reply-form" (ngSubmit)="reply()">
                    <label>{{ i18n.t('comments.replyLabel') }}
                      <textarea name="reply" rows="2" [maxlength]="maxLength" [(ngModel)]="replyDraft"></textarea>
                    </label>
                    <div class="admin-actions">
                      <button class="button secondary" type="button" (click)="replyingTo.set(null)">{{ i18n.t('common.cancel') }}</button>
                      <button class="button" type="submit" [disabled]="busy() || !replyDraft.trim()">{{ i18n.t('comments.reply') }}</button>
                    </div>
                  </form>
                }
              </li>
            }
          </ol>
        } @else {
          <p class="muted">{{ i18n.t('comments.empty') }}</p>
        }
      } @else if (!loadFailed()) {
        <p class="muted" role="status">{{ i18n.t('comments.loading') }}</p>
      }
    </section>

    <ng-template #commentTpl let-comment>
      <article class="comment" [class.deleted]="comment.isDeleted">
        @if (comment.isDeleted) {
          <p class="muted comment-deleted">{{ i18n.t('comments.deleted') }}</p>
        } @else {
          <header class="comment-meta">
            <strong>{{ comment.authorName }}</strong>
            <time [attr.datetime]="comment.createdAt">{{ comment.createdAt | date:'medium' }}</time>
            @if (comment.updatedAt) { <span class="muted">· {{ i18n.t('comments.edited') }}</span> }
          </header>
          @if (editingId() === comment.id) {
            <form class="comment-form" (ngSubmit)="saveEdit(comment)">
              <label class="visually-hidden" [for]="'edit-' + comment.id">{{ i18n.t('common.edit') }}</label>
              <textarea [id]="'edit-' + comment.id" name="edit" rows="3" [maxlength]="maxLength" [(ngModel)]="editDraft"></textarea>
              <div class="admin-actions">
                <button class="button secondary" type="button" (click)="editingId.set(null)">{{ i18n.t('common.cancel') }}</button>
                <button class="button" type="submit" [disabled]="busy() || !editDraft.trim()">{{ i18n.t('common.save') }}</button>
              </div>
            </form>
          } @else {
            <p class="comment-body">{{ comment.body }}</p>
            @if (confirmingDeleteId() === comment.id) {
              <div class="inline-confirm" role="alertdialog" [attr.aria-label]="i18n.t('comments.deleteConfirm')">
                <span>{{ i18n.t('comments.deleteConfirm') }}</span>
                <button class="button secondary" type="button" (click)="confirmingDeleteId.set(null)">{{ i18n.t('common.cancel') }}</button>
                <button class="button danger" type="button" [disabled]="busy()" (click)="remove(comment)">{{ i18n.t('common.delete') }}</button>
              </div>
            } @else {
              <div class="entry-actions">
                @if (auth.currentUser()?.displayName) {
                  <button class="link-button" type="button" (click)="startReply(comment)">{{ i18n.t('comments.reply') }}</button>
                }
                @if (comment.canEdit) {
                  <button class="link-button" type="button" (click)="startEdit(comment)">{{ i18n.t('common.edit') }}</button>
                }
                @if (comment.canDelete) {
                  <button class="link-button danger-link" type="button" (click)="confirmingDeleteId.set(comment.id)">{{ i18n.t('common.delete') }}</button>
                }
              </div>
            }
          }
        }
      </article>
    </ng-template>
  `
})
export class RecipeCommentsComponent {
  private readonly api = inject(CommentApiService);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly recipeId = input.required<string>();
  readonly maxLength = commentMaxLength;
  readonly threads = signal<RecipeComment[] | null>(null);
  readonly loadFailed = signal(false);
  readonly busy = signal(false);
  readonly error = signal<TranslationKey | null>(null);
  readonly replyingTo = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);
  readonly confirmingDeleteId = signal<string | null>(null);
  draft = '';
  replyDraft = '';
  editDraft = '';

  constructor() {
    effect(() => {
      const id = this.recipeId();
      untracked(() => this.load(id));
    });
  }

  threadOf(commentId: string): string | undefined {
    return this.threads()?.find(thread => thread.id === commentId || thread.replies.some(reply => reply.id === commentId))?.id;
  }

  startReply(comment: RecipeComment): void {
    this.editingId.set(null);
    this.replyDraft = '';
    this.replyingTo.set(comment.id);
  }

  startEdit(comment: RecipeComment): void {
    this.replyingTo.set(null);
    this.editDraft = comment.body;
    this.editingId.set(comment.id);
  }

  post(): void {
    this.write(this.api.add(this.recipeId(), this.draft), 'comments.postFailed', () => (this.draft = ''));
  }

  reply(): void {
    const parentId = this.replyingTo();
    if (!parentId) return;
    this.write(this.api.add(this.recipeId(), this.replyDraft, parentId), 'comments.postFailed', () => {
      this.replyDraft = '';
      this.replyingTo.set(null);
    });
  }

  saveEdit(comment: RecipeComment): void {
    this.write(this.api.update(comment.id, this.editDraft), 'comments.saveFailed', () => this.editingId.set(null));
  }

  remove(comment: RecipeComment): void {
    this.write(this.api.delete(comment.id), 'comments.deleteFailed', () => this.confirmingDeleteId.set(null));
  }

  private write(request: Observable<unknown>, failure: TranslationKey, done: () => void): void {
    this.busy.set(true);
    this.error.set(null);
    request.subscribe({
      next: () => {
        this.busy.set(false);
        done();
        this.load(this.recipeId());
      },
      error: (response: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(response.status === 409 ? 'comments.nameRequired' : failure);
        // The thread may have changed underneath us (e.g. a parent was deleted).
        if (response.status === 404) this.load(this.recipeId());
      }
    });
  }

  private load(recipeId: string): void {
    this.api.list(recipeId).subscribe({
      next: threads => {
        this.threads.set(threads);
        this.loadFailed.set(false);
      },
      error: () => {
        this.threads.set(null);
        this.loadFailed.set(true);
        this.error.set('comments.loadFailed');
      }
    });
  }
}
