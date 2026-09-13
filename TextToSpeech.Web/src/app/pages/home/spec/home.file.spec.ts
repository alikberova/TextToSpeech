import { ComponentFixture } from '@angular/core/testing';
import { Signal } from '@angular/core';
import { HomePage } from '../home.page';
import { By } from '@angular/platform-browser';
import { describe, beforeEach, it, expect } from 'vitest';
import { createHomeFixture } from './home.page.spec-setup';
import { DEFAULT_FILE_CONTENT, DEFAULT_FILE_NAME } from './test-data';

describe('HomePage - File interactions', () => {
    let fixture: ComponentFixture<HomePage>;
    let component: HomePage;

    beforeEach(async () => {
        const created = await createHomeFixture();
        fixture = created.fixture;
        component = created.component;
    });

    it('onFileSelected stores single file and marks touched', async () => {
        const input = document.createElement('input');
        const dataTransfer = new DataTransfer();
        const file = new File([DEFAULT_FILE_CONTENT], DEFAULT_FILE_NAME);
        dataTransfer.items.add(file);
        Object.defineProperty(input, 'files', { value: dataTransfer.files, writable: false });

        component.onFileSelected(input);
        fixture.detectChanges();

        expect(component.file()).toBeTruthy();
        expectFileTouched(component);
    });

    it('file drag/drop sets file; remove sets touched', async () => {
        // Simulate drop
        const dropzone = fixture.debugElement.query(By.css('.dropzone'));
        const file = new File([DEFAULT_FILE_CONTENT], DEFAULT_FILE_NAME, { type: 'text/plain' });
        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        dropzone.triggerEventHandler('drop', { preventDefault: () => undefined, dataTransfer });
        fixture.detectChanges();
        expect(component.file()).not.toBeNull();

        // Remove file -> touched remains true
        component.removeFile();
        fixture.detectChanges();
        expect(component.file()).toBeNull();
        expectFileTouched(component);
    });

});

function expectFileTouched(component: HomePage) {
    interface HomePageAccess {
        fileTouched: Signal<boolean>;
    }
    const access = component as unknown as HomePage & HomePageAccess;
    expect(access.fileTouched()).toBe(true);
}

class FakeDataTransfer {
  items = {
    files: [] as File[],
    add: (file: File) => this.files.push(file),
    remove: (index: number) => this.files.splice(index, 1),
    get length() { return this.files.length; }
  };

  get files() {
    return this.items.files;
  }
}

(globalThis as unknown as { DataTransfer: typeof FakeDataTransfer }).DataTransfer =
    FakeDataTransfer;