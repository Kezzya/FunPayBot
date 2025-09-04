Для связи со мной по боту пишите в мой тг: [@kezzya](t.me/kezzya)
 
 # FunPayBot

Веб-приложение для автоматизации работы с торговой площадкой FunPay через API.

## О проекте

FunPayBot - это система, состоящая из двух частей:
- **ASP.NET Core** веб-приложение с удобным интерфейсом
- **FastAPI** микросервис для работы с Python библиотекой FunPayAPI

## Возможности

- 🔐 **Авторизация** через golden_key от FunPay
- 📦 **Получение лотов** по категориям и подкатегориям
- 📊 **Мониторинг продаж** и покупок
- 📝 **Логирование** всех операций (Serilog + Seq)
- 🎯 **Swagger UI** для тестирования API

## Технологический стек

**Backend:**
- ASP.NET Core 8
- Entity Framework Core + PostgreSQL
- Serilog для логирования
- Seq для просмотра логов

**Python микросервис:**
- FastAPI
- FunPayAPI библиотека
- Uvicorn сервер

## Архитектура

```
[Браузер] → [ASP.NET Core :7151] → [FastAPI :8000] → [FunPay API]
                ↓
            [PostgreSQL]
                ↓
             [Seq Logs]
```

## Быстрый старт

### 1. Python сервис
```bash
cd python-service/
python -m venv venv
venv\Scripts\activate
pip install fastapi uvicorn FunPayAPI lxml beautifulsoup4
uvicorn main:app --reload --port 8000
```

### 2. ASP.NET Core приложение
```bash
dotnet restore
dotnet run
```

### 3. Открыть в браузере
- Приложение: https://localhost:7151/
- API документация: https://localhost:7151/swagger
- Логи: http://localhost:5341/ (если запущен Seq)

## Использование

1. Получите golden_key из FunPay (через браузер)
2. Используйте API для авторизации и работы с лотами
3. Все операции логируются в Seq для отладки

## API Endpoints

- `POST /api/auth` - авторизация в FunPay
- `GET /api/lots/{subcategoryId}` - получение лотов по подкатегории

---

*Проект создан для автоматизации торговли на FunPay*

