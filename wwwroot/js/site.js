// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ✅ Toast tiện ích
function showToast(message) {
    const toastEl = document.getElementById('toastMessage');
    if (toastEl) {
        toastEl.querySelector('.toast-body').innerText = message;
        new bootstrap.Toast(toastEl, { delay: 3000 }).show();
    }
}

function showErrorToast(message) {
    const errorToastEl = document.getElementById('errorToast');
     if (errorToastEl) {
        errorToastEl.querySelector('.toast-body').innerText = message;
        new bootstrap.Toast(errorToastEl, { delay: 4000 }).show();
     }
}
