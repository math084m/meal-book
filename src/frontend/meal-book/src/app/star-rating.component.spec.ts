import { TestBed } from '@angular/core/testing';
import { StarRatingComponent } from './star-rating.component';

describe('StarRatingComponent', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({ imports: [StarRatingComponent] }).compileComponents();
  });

  function create(value: number | null) {
    const fixture = TestBed.createComponent(StarRatingComponent);
    fixture.componentRef.setInput('value', value);
    fixture.detectChanges();
    const buttons = () => Array.from(fixture.nativeElement.querySelectorAll('button.star')) as HTMLButtonElement[];
    return { fixture, component: fixture.componentInstance, buttons };
  }

  it('selects a star on click and clears it when clicked again', () => {
    const { fixture, component, buttons } = create(null);
    buttons()[2].click();
    fixture.detectChanges();
    expect(component.value()).toBe(3);
    expect(buttons()[2].getAttribute('aria-checked')).toBe('true');
    expect(buttons().filter(button => button.classList.contains('filled')).length).toBe(3);

    buttons()[2].click();
    expect(component.value()).toBeNull();
  });

  it('keeps exactly one star in the tab order and moves it with the arrow keys', () => {
    const { fixture, component, buttons } = create(4);
    expect(buttons().map(button => button.tabIndex)).toEqual([-1, -1, -1, 0, -1]);

    const group = fixture.nativeElement.querySelector('[role="radiogroup"]') as HTMLElement;
    group.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    expect(component.value()).toBe(5);
    group.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    expect(component.value()).toBe(5);
    group.dispatchEvent(new KeyboardEvent('keydown', { key: 'Home', bubbles: true }));
    expect(component.value()).toBe(1);
    group.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
    expect(component.value()).toBe(1);
  });

  it('renders a labelled, non-interactive rating in read-only mode', () => {
    const fixture = TestBed.createComponent(StarRatingComponent);
    fixture.componentRef.setInput('value', 3);
    fixture.componentRef.setInput('readonly', true);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('button')).toBeNull();
    expect(element.querySelector('[role="img"]')?.getAttribute('aria-label')).toBe('3 of 5 stars');
  });
});
