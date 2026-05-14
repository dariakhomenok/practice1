// Форматирование даты
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('ru-RU');
}

// Форматирование цены
function formatPrice(price) {
    return new Intl.NumberFormat('ru-RU', {
        style: 'currency',
        currency: 'RUB',
        minimumFractionDigits: 0
    }).format(price);
}

// Получение параметров из URL
function getUrlParams() {
    const params = new URLSearchParams(window.location.search);
    const result = {};
    for (const [key, value] of params) {
        result[key] = value;
    }
    return result;
}

// Валидация email
function isValidEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

// Валидация телефона (простая)
function isValidPhone(phone) {
    const re = /^[\d\s\+\-\(\)]{10,20}$/;
    return re.test(phone);
}

// Показать уведомление
function showNotification(message, type = 'success') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type}`;
    notification.textContent = message;
    notification.style.position = 'fixed';
    notification.style.top = '20px';
    notification.style.right = '20px';
    notification.style.zIndex = '9999';
    notification.style.minWidth = '300px';
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 3000);
}

// Показать загрузку
function showLoader(containerId) {
    const container = document.getElementById(containerId);
    if (container) {
        container.innerHTML = '<div class="loader"></div>';
    }
}

// Скрыть загрузку (просто очистить)
function hideLoader(containerId) {
    const container = document.getElementById(containerId);
    if (container) {
        container.innerHTML = '';
    }
}

// Сохранить данные пользователя после входа
function saveUserData(userData) {
    localStorage.setItem('token', userData.token);
    localStorage.setItem('userId', userData.userId);
    localStorage.setItem('userName', userData.userName);
}

// Проверка, является ли пользователь арендодателем
// (можно определить по URL или по специальному флагу)
function isLandlord() {
    return window.location.pathname.includes('/landlord/');
}

// Перенаправление на страницу входа
function redirectToLogin() {
    if (isLandlord()) {
        window.location.href = '/landlord/login.html';
    } else {
        window.location.href = '/tenant/login.html';
    }
}

// Проверка авторизации для защищенных страниц
function requireAuth() {
    if (!AuthAPI.isAuthenticated()) {
        redirectToLogin();
        return false;
    }
    return true;
}