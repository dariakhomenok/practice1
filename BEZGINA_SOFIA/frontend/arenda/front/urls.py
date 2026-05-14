from django.urls import path
from . import views

urlpatterns = [
    path('', views.index_page, name='index'),
    path('register/', views.register_page, name='register'),
    path('login/', views.login_page, name='login'),
    path('search/', views.search_page, name='search'),
    path('apartment/<int:apartment_id>/', views.apartment_page, name='apartment'),
    path('booking/', views.booking_page, name='booking'),
    path('my-bookings/', views.my_bookings_page, name='my_bookings'),
    path('profile/', views.profile_page, name='profile'),
    path('my-apartments/', views.my_apartments_page, name='my_apartments'),
    path('add-apartment/', views.add_apartment_page, name='add_apartment'),
    path('edit-apartment/<int:apartment_id>/', views.edit_apartment_page, name='edit_apartment'),
    path('landlord-bookings/', views.landlord_bookings_page, name='landlord_bookings'),
]