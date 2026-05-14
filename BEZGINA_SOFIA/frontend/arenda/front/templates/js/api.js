// Базовый URL вашего бэкенда
const API_BASE_URL = 'https://localhost:7151/api';

// Общая функция для всех запросов к API
async function apiRequest(endpoint, method = 'GET', data = null) {
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json'
        }
    };
    
    if (data) {
        options.body = JSON.stringify(data);
    }
    
    // Добавляем токен авторизации, если пользователь вошёл
    const token = localStorage.getItem('token');
    if (token) {
        options.headers['Authorization'] = `Bearer ${token}`;
    }
    
    try {
        const response = await fetch(API_BASE_URL + endpoint, options);
        const result = await response.json();
        
        if (!response.ok) {
            throw new Error(result.message || 'Ошибка запроса');
        }
        
        return result;
    } catch (error) {
        console.error('API Error:', error);
        throw error;
    }
}

// Функции для работы с аутентификацией
const AuthAPI = {
    // Вход (использует ваш /Auth/login)
    login: async (login, password) => {
        return apiRequest('/Auth/login', 'POST', { login, password });  // ← большая А
    },
    
    // ЕДИНАЯ регистрация (использует ваш /Auth/register)
    register: async (userData) => {
        return apiRequest('/Auth/register', 'POST', userData);  // ← большая А
    },
    
    // Выход
    logout: () => {
        localStorage.removeItem('token');
        localStorage.removeItem('userId');
        localStorage.removeItem('userName');
        localStorage.removeItem('isLandlord');
        window.location.href = '/';
    },
    
    // Проверка авторизации
    isAuthenticated: () => {
        return !!localStorage.getItem('token');
    }
};

// Функции для работы с пользователями
const UserAPI = {
    // Получить данные пользователя (нужно добавить на бэкенде)
    getProfile: async (userId) => {
        return apiRequest(`/users/${userId}`);  // пока не работает
    },
    
    // Обновить профиль (нужно добавить на бэкенде)
    updateProfile: async (userId, data) => {
        return apiRequest(`/users/${userId}`, 'PUT', data);  // пока не работает
    }
};

// Функции для работы с квартирами (нужно добавить на бэкенде)
const ApartmentAPI = {
    // Получить все квартиры
    getAll: async () => {
        return apiRequest('/apartments');  // пока не работает
    },
    
    // Получить квартиру по ID
    getById: async (id) => {
        return apiRequest(`/apartments/${id}`);  // пока не работает
    },
    
    // Остальные методы пока не работают
};

// Функции для работы с бронированиями (нужно добавить на бэкенде)
const BookingAPI = {
    // Создать бронь
    create: async (data) => {
        return apiRequest('/bookings', 'POST', data);  // пока не работает
    }
};