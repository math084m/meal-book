import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { adminGuard, userGuard } from './auth.guard';

// Everything except the landing page is lazy-loaded to keep the initial bundle small.
export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'recipes', loadComponent: () => import('./recipe-list.component').then(m => m.RecipeListComponent) },
  { path: 'recipes/:id', loadComponent: () => import('./recipe-detail.component').then(m => m.RecipeDetailComponent) },
  { path: 'recipes/:id/cook', loadComponent: () => import('./cook-mode.component').then(m => m.CookModeComponent) },
  { path: 'login', loadComponent: () => import('./login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./register.component').then(m => m.RegisterComponent) },
  { path: 'history', loadComponent: () => import('./cooking-history.component').then(m => m.CookingHistoryComponent), canActivate: [userGuard] },
  { path: 'plan', loadComponent: () => import('./plan.component').then(m => m.PlanComponent), canActivate: [userGuard] },
  { path: 'admin/recipes/new', loadComponent: () => import('./admin-recipe-form.component').then(m => m.AdminRecipeFormComponent), canActivate: [adminGuard] },
  { path: 'admin/recipes/drafts', loadComponent: () => import('./admin-drafts.component').then(m => m.AdminDraftsComponent), canActivate: [adminGuard] },
  { path: 'admin/ingredients', loadComponent: () => import('./admin-ingredients.component').then(m => m.AdminIngredientsComponent), canActivate: [adminGuard] },
  { path: 'admin/recipes/:id/edit', loadComponent: () => import('./admin-recipe-form.component').then(m => m.AdminRecipeFormComponent), canActivate: [adminGuard] },
  { path: '**', redirectTo: '' }
];
