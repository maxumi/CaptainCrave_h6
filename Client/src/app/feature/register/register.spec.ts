import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Register } from './register';
import { getTranslocoModule } from '../../shared/test/transloco-testing.module';
import { provideRouter } from '@angular/router';

describe('Register', () => {
  let component: Register;
  let fixture: ComponentFixture<Register>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Register,getTranslocoModule()],
      providers: [provideRouter([])]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Register);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
