import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Login } from './login';
import { getTranslocoModule } from '../../shared/test/transloco-testing.module';
import { provideRouter } from '@angular/router';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login, getTranslocoModule()],
      providers: [provideRouter([])]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
