import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Restaurants } from './restaurants';
import { getTranslocoModule } from '../../shared/test/transloco-testing.module';
import { provideRouter } from '@angular/router';

describe('Restaurants', () => {
  let component: Restaurants;
  let fixture: ComponentFixture<Restaurants>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Restaurants, getTranslocoModule()],
      providers: [provideRouter([])]

    })
    .compileComponents();

    fixture = TestBed.createComponent(Restaurants);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
