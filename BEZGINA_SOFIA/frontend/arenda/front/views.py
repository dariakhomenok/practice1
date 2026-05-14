from django.shortcuts import render
import requests  # для запросов к вашему API
import json

# Create your views here.

def register_page(request):
    """Страница регистрации"""
    return render(request, 'register.html')

def login_page(request):
    """Страница входа"""
    return render(request, 'login.html')

def index_page(request):
    """Главная страница"""
    return render(request, 'index.html')

def search_page(request):
    """Страница поиска"""
    return render(request, 'search.html')

def apartment_page(request, apartment_id):
    """Страница квартиры"""
    return render(request, 'apartment.html', {'apartment_id': apartment_id})

def booking_page(request):
    """Страница бронирования"""
    apartment_id = request.GET.get('id')
    check_in = request.GET.get('checkIn')
    check_out = request.GET.get('checkOut')
    return render(request, 'booking.html', {
        'apartment_id': apartment_id,
        'check_in': check_in,
        'check_out': check_out
    })

def my_bookings_page(request):
    """Мои бронирования"""
    return render(request, 'my-bookings.html')

def profile_page(request):
    """Профиль пользователя"""
    return render(request, 'profile.html')

def my_apartments_page(request):
    """Мои квартиры (для арендодателя)"""
    return render(request, 'my-apartments.html')

def add_apartment_page(request):
    """Добавление квартиры"""
    return render(request, 'add-apartment.html')

def edit_apartment_page(request, apartment_id):
    return render(request, 'edit-apartment.html', {'apartment_id': apartment_id})

def landlord_bookings_page(request):
    """Бронирования квартир арендодателя"""
    return render(request, 'landlord-bookings.html')