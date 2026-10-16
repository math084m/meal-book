import { Routes } from '@angular/router';
import { HomeComponent } from './home.component';
import { RecipeListComponent } from './recipe-list.component';
import { RecipeDetailComponent } from './recipe-detail.component';
import { LoginComponent } from './login.component';
import { RegisterComponent } from './register.component';
import { CookingHistoryComponent } from './cooking-history.component';
import { AdminRecipeFormComponent } from './admin-recipe-form.component';
import { adminGuard, userGuard } from './auth.guard';
import { AdminDraftsComponent } from './admin-drafts.component';
import { AdminIngredientsComponent } from './admin-ingredients.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'recipes', component: RecipeListComponent },
  { path: 'recipes/:id', component: RecipeDetailComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'history', component: CookingHistoryComponent, canActivate: [userGuard] },
  { path: 'admin/recipes/new', component: AdminRecipeFormComponent, canActivate: [adminGuard] },
  { path: 'admin/recipes/drafts', component: AdminDraftsComponent, canActivate: [adminGuard] },
  { path: 'admin/ingredients', component: AdminIngredientsComponent, canActivate: [adminGuard] },
  { path: 'admin/recipes/:id/edit', component: AdminRecipeFormComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: '' }
];
