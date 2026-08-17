import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { getTranslocoModule } from './shared/test/transloco-testing.module';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App, getTranslocoModule()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });
});
