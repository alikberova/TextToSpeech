import { TestBed } from '@angular/core/testing';
import { HttpRequest, HttpHandlerFn, HttpResponse, HttpEvent, } from '@angular/common/http';
import { of } from 'rxjs';
import { API_URL } from '../../../constants/tokens';
import { baseUrlInterceptor } from './api-url-interceptor';
import { getZonelessProviders } from '../../../../testing/spec-test-utils';
import { describe, beforeEach, it, expect } from 'vitest';

describe('baseUrlInterceptor', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: getZonelessProviders([
                { provide: API_URL, useValue: 'https://api.example.com' },
            ]),
        });
    });

    it('should prefix relative URLs with the base apiUrl', async () => {
        const interceptorFn = baseUrlInterceptor;

        const req = new HttpRequest('GET', '/foo');
        const next: HttpHandlerFn = (r: HttpRequest<unknown>) => of(new HttpResponse({ status: 200, url: r.url }));

        TestBed.runInInjectionContext(() => {
            interceptorFn(req, next).subscribe((event: HttpEvent<unknown>) => {
                if (event instanceof HttpResponse) {
                    expect(event.url).toBe('https://api.example.com/foo');
                    ;
                }
            });
        });
    });

    it('should not modify absolute URLs', async () => {
        const interceptorFn = baseUrlInterceptor;

        const req = new HttpRequest('GET', 'https://other.host/bar');
        const next: HttpHandlerFn = (r: HttpRequest<unknown>) => of(new HttpResponse({ status: 200, url: r.url }));

        TestBed.runInInjectionContext(() => {
            interceptorFn(req, next).subscribe((event: HttpEvent<unknown>) => {
                if (event instanceof HttpResponse) {
                    expect(event.url).toBe('https://other.host/bar');
                }
            });
        });
    });
});
