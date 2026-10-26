import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// jsdom no implementa estas APIs del navegador; se simulan sin comportamiento.
Element.prototype.scrollIntoView = function scrollIntoView() {}
HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
  this.open = true
}
HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
  this.open = false
}

afterEach(() => {
  cleanup()
  localStorage.clear()
})
