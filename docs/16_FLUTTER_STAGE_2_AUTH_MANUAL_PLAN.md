# Flutter Stage 2. Авторизация вручную

Этот документ описывает, как вручную выполнить второй этап Flutter-разработки в приложении SwirlApp.

План рассчитан на начинающего Flutter-разработчика. Здесь нет сложной Clean Architecture: нужно просто разделить модели, API-запросы, состояние и экраны по уже существующим папкам проекта.

Важно: этот этап касается только авторизации. Не нужно добавлять обучение словам, уровни, профиль, daily test, достижения, тёмную тему, iOS, web или другие MVP-фичи.

## 1. Цель второго этапа

Цель Stage 2 - сделать вход, регистрацию и проверку текущего пользователя через backend.

Пользователь должен уметь:

- открыть приложение;
- попасть на стартовый экран, если он ещё не авторизован;
- зарегистрироваться;
- выбрать аватар при регистрации;
- войти по email и паролю;
- остаться авторизованным после перезапуска приложения;
- автоматически попасть на Home, если сохранённый JWT ещё валиден;
- выйти из аккаунта позже, когда logout будет подключён к UI.

На этом этапе Flutter должен начать работать с реальным backend API, а не с заглушками.

## 2. Что должно получиться в конце

В конце Stage 2 должно быть так:

- `SplashScreen` проверяет сохранённый JWT.
- Если токена нет, приложение открывает `/first`.
- Если токен есть, Flutter вызывает `GET /api/auth/me`.
- Если backend подтвердил токен, приложение открывает `/home`.
- Если токен невалидный или истёк, Flutter удаляет токен и открывает `/first`.
- `FirstScreen` показывает выбор: войти или зарегистрироваться.
- `LoginScreen` отправляет email и пароль на backend.
- `SignUpScreen` загружает аватары и отправляет данные регистрации.
- После успешного login/register токен сохраняется в `flutter_secure_storage`.
- Все защищённые запросы автоматически отправляют заголовок `Authorization: Bearer <token>`.
- Ошибки показываются понятным текстом, а не сырыми исключениями.
- Формы не ломаются на маленьком Android-экране и при открытой клавиатуре.
- `flutter analyze` проходит без ошибок.

## 3. Backend endpoints, которые используются

На Stage 2 нужны только четыре endpoint.

### GET /api/avatars

Загружает список доступных аватаров для регистрации.

Пример ответа:

```json
[
  {
    "id": 1,
    "name": "Avatar 1",
    "imageUrl": "/media/avatars/avatar_1.png"
  }
]
```

Особенности:

- в текущем backend endpoint публичный;
- токен для него не нужен;
- Flutter не должен ожидать фиксированное количество аватаров;
- изображения приходят как относительные пути, например `/media/avatars/avatar_1.png`;
- полный URL нужно собирать через backend origin, например `http://10.0.2.2:5000/media/avatars/avatar_1.png`.

### POST /api/auth/register

Регистрирует нового пользователя.

Тело запроса:

```json
{
  "name": "Vladimir",
  "email": "user@example.com",
  "password": "password123",
  "confirmPassword": "password123",
  "avatarId": 1
}
```

Пример ответа:

```json
{
  "accessToken": "jwt-token",
  "user": {
    "id": "uuid",
    "name": "Vladimir",
    "email": "user@example.com",
    "avatarUrl": "/media/avatars/avatar_1.png"
  }
}
```

После успешной регистрации нужно сохранить `accessToken` и перейти на `/home`.

### POST /api/auth/login

Авторизует существующего пользователя.

Тело запроса:

```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

Пример ответа такой же, как у регистрации:

```json
{
  "accessToken": "jwt-token",
  "user": {
    "id": "uuid",
    "name": "Vladimir",
    "email": "user@example.com",
    "avatarUrl": "/media/avatars/avatar_1.png"
  }
}
```

После успешного входа нужно сохранить `accessToken` и перейти на `/home`.

### GET /api/auth/me

Возвращает текущего пользователя по JWT.

Пример ответа:

```json
{
  "id": "uuid",
  "name": "Vladimir",
  "email": "user@example.com",
  "avatarUrl": "/media/avatars/avatar_1.png"
}
```

Особенности:

- endpoint требует JWT;
- используется на `SplashScreen`, чтобы проверить сохранённый токен;
- если backend вернул `401 Unauthorized`, токен нужно удалить.

## 4. Какие файлы нужно создать или изменить

Работать нужно внутри Flutter-проекта:

```text
frontend/swirl_app/
```

Не нужно менять backend.

### Уже существующие файлы, которые нужно проверить и при необходимости изменить

```text
lib/core/network/api_client.dart
lib/core/storage/token_storage.dart
lib/core/errors/api_error.dart
lib/data/api/api_paths.dart
lib/presentation/state/app_state_providers.dart
lib/app/router.dart
lib/presentation/screens/splash_screen.dart
lib/presentation/screens/first_screen.dart
lib/presentation/screens/login_screen.dart
lib/presentation/screens/signup_screen.dart
lib/presentation/screens/home_screen.dart
```

### Файлы, которые нужно создать для Stage 2

Рекомендуемый минимум:

```text
lib/domain/models/user_model.dart
lib/domain/models/auth_response_model.dart
lib/domain/models/avatar_model.dart
lib/data/api/auth_api.dart
lib/presentation/state/auth_provider.dart
lib/presentation/state/avatars_provider.dart
```

Если в проекте уже есть похожие файлы или модели, не дублируйте их. Лучше аккуратно привести существующие к нужному виду.

Например, сейчас может уже быть `lib/domain/models/app_user.dart`. Тогда есть два нормальных варианта:

- оставить `AppUser`, если он уже используется, но убедиться, что он умеет создаваться из JSON;
- или создать `UserModel` и постепенно использовать его в авторизации.

Главное - не плодить две разные модели пользователя без необходимости.

## 5. Какие модели создать

Модели нужны, чтобы не работать с `Map<String, dynamic>` прямо в экранах.

### UserModel

Назначение: хранит данные текущего пользователя.

Поля:

```text
id: String
name: String
email: String
avatarUrl: String?
```

Нужен метод `fromJson`, который читает:

- `id`;
- `name`;
- `email`;
- `avatarUrl`.

Важно: backend не возвращает password и password hash. Flutter тоже не должен их хранить.

### AuthResponseModel

Назначение: хранит ответ от login/register.

Поля:

```text
accessToken: String
user: UserModel
```

Нужен метод `fromJson`, который читает:

- `accessToken`;
- вложенный объект `user`.

### AvatarModel

Назначение: хранит один аватар из `GET /api/avatars`.

Поля:

```text
id: int
name: String
imageUrl: String
```

Нужен метод `fromJson`.

Для показа картинки в UI не используйте `imageUrl` напрямую, если там относительный путь. Сначала соберите полный media URL через существующий helper, например `MediaUrlBuilder`, если он уже есть в проекте.

### ApiError

В проекте уже может быть файл:

```text
lib/core/errors/api_error.dart
```

Проверьте, что модель умеет читать backend error format:

```json
{
  "error": {
    "code": "validation_error",
    "message": "Validation failed",
    "details": {
      "email": ["Email is required"]
    }
  }
}
```

Минимально нужны поля:

```text
code: String
message: String
details: Map<String, dynamic>?
```

Если backend вернул неизвестный формат, приложение должно показать обычное дружелюбное сообщение, например:

```text
Что-то пошло не так. Попробуйте ещё раз.
```

## 6. Как сделать AuthApi

Создайте файл:

```text
lib/data/api/auth_api.dart
```

`AuthApi` должен принимать `Dio` через конструктор.

Пример структуры:

```dart
class AuthApi {
  AuthApi(this._dio);

  final Dio _dio;

  Future<List<AvatarModel>> getAvatars() async {}

  Future<AuthResponseModel> login({
    required String email,
    required String password,
  }) async {}

  Future<AuthResponseModel> register({
    required String name,
    required String email,
    required String password,
    required String confirmPassword,
    required int avatarId,
  }) async {}

  Future<UserModel> me() async {}
}
```

### getAvatars

Должен отправлять:

```text
GET /api/avatars
```

Через `ApiPaths` путь обычно будет:

```text
/avatars
```

Логика:

1. Вызвать `_dio.get(ApiPaths.avatars)`.
2. Проверить, что `response.data` - список.
3. Каждый элемент списка превратить в `AvatarModel.fromJson`.
4. Вернуть `List<AvatarModel>`.

Если список пустой, это не crash. На экране регистрации нужно показать пустое состояние или использовать регистрацию без выбора аватара только если backend это разрешает. По контракту лучше требовать выбор аватара.

### login

Должен отправлять:

```text
POST /api/auth/login
```

Тело:

```dart
{
  'email': email,
  'password': password,
}
```

Логика:

1. Вызвать `_dio.post(ApiPaths.authLogin, data: ...)`.
2. Ответ превратить в `AuthResponseModel`.
3. Вернуть модель наверх в provider.
4. Не сохранять токен внутри `AuthApi`, если сохранение уже делает `authProvider`. Так проще тестировать и понимать код.

### register

Должен отправлять:

```text
POST /api/auth/register
```

Тело:

```dart
{
  'name': name,
  'email': email,
  'password': password,
  'confirmPassword': confirmPassword,
  'avatarId': avatarId,
}
```

Логика такая же, как у login:

1. Отправить запрос.
2. Распарсить `AuthResponseModel`.
3. Вернуть результат.

### me

Должен отправлять:

```text
GET /api/auth/me
```

Логика:

1. Вызвать `_dio.get(ApiPaths.authMe)`.
2. Ответ превратить в `UserModel`.
3. Вернуть текущего пользователя.

JWT вручную добавлять в этот метод не нужно, если уже настроен Dio interceptor.

## 7. Как работать с JWT

JWT - это токен доступа. Backend выдаёт его после login/register. Flutter хранит токен и отправляет его в защищённые endpoints.

### Где хранить токен

Используйте `flutter_secure_storage`.

В проекте уже есть подходящий файл:

```text
lib/core/storage/token_storage.dart
```

Ключ хранения:

```text
swirl_access_token
```

### Сохранить токен после login/register

После успешного ответа от backend:

1. взять `authResponse.accessToken`;
2. вызвать `tokenStorage.saveAccessToken(token)`;
3. обновить auth state;
4. перейти на `/home`.

Не храните email/password после входа.

### Читать токен на SplashScreen

Когда приложение стартует:

1. `SplashScreen` вызывает метод проверки авторизации из `authProvider`;
2. provider читает токен через `tokenStorage.readAccessToken()`;
3. если токена нет, результат - пользователь не авторизован;
4. если токен есть, provider вызывает `AuthApi.me()`;
5. если `me()` успешен, пользователь авторизован;
6. если `me()` вернул `401`, токен удаляется.

`SplashScreen` должен быть очень простым: показать loading и после проверки сделать переход.

### Удалять токен при logout

Logout должен делать три вещи:

1. `tokenStorage.deleteAccessToken()`;
2. очистить текущего пользователя в `authProvider`;
3. перейти на `/first`.

Даже если logout-кнопка будет полноценно сделана на Stage 3 в Profile, сам метод `logout()` в `authProvider` лучше подготовить на Stage 2.

### Отправлять токен через Dio interceptor

Проверьте `lib/core/network/api_client.dart`.

В `onRequest` interceptor должен быть такой смысл:

```dart
final token = await tokenStorage.readAccessToken();
if (token != null && token.isNotEmpty) {
  options.headers['Authorization'] = 'Bearer $token';
}
```

Так токен автоматически попадёт в `GET /api/auth/me` и будущие защищённые запросы.

В `onError`, если backend вернул `401`, можно удалить токен:

```dart
if (error.response?.statusCode == 401) {
  await tokenStorage.deleteAccessToken();
}
```

Но одного удаления мало для UI. `authProvider` тоже должен понять, что пользователь больше не авторизован, и отправить его на `/first` или `/login`.

## 8. Как сделать state через Riverpod

На Stage 2 нужны два основных provider:

- `authProvider`;
- `avatarsProvider`.

### authProvider

Создайте файл:

```text
lib/presentation/state/auth_provider.dart
```

Рекомендуемый простой вариант - `AsyncNotifier<UserModel?>`.

Состояния:

```text
loading - идёт проверка, login или register
data(user) - пользователь авторизован
data(null) - пользователь не авторизован
error - произошла ошибка
```

Методы, которые нужны:

```text
checkAuth()
login(email, password)
register(name, email, password, confirmPassword, avatarId)
logout()
```

Что делает `checkAuth()`:

1. читает токен;
2. если токена нет, ставит `state = AsyncData(null)`;
3. если токен есть, вызывает `AuthApi.me()`;
4. если успешно, ставит `state = AsyncData(user)`;
5. если ошибка `401`, удаляет токен и ставит `state = AsyncData(null)`;
6. если сеть или backend недоступны, ставит error, чтобы Splash мог показать retry.

Что делает `login()`:

1. ставит loading;
2. вызывает `AuthApi.login`;
3. сохраняет `accessToken`;
4. ставит `state = AsyncData(user)`;
5. экран после этого переходит на `/home`.

Что делает `register()`:

1. ставит loading;
2. вызывает `AuthApi.register`;
3. сохраняет `accessToken`;
4. ставит `state = AsyncData(user)`;
5. экран после этого переходит на `/home`.

Что делает `logout()`:

1. удаляет token;
2. ставит `state = AsyncData(null)`;
3. UI делает переход на `/first`.

### avatarsProvider

Создайте файл:

```text
lib/presentation/state/avatars_provider.dart
```

Рекомендуемый простой вариант:

```text
FutureProvider<List<AvatarModel>>
```

Логика:

1. взять `AuthApi`;
2. вызвать `getAvatars()`;
3. вернуть список.

На `SignUpScreen` используйте `ref.watch(avatarsProvider)`.

Показывайте:

- loading, пока аватары грузятся;
- список аватаров, если всё хорошо;
- ошибку и кнопку retry, если запрос не удался;
- пустое состояние, если backend вернул пустой список.

## 9. Как сделать экраны

На Stage 2 нужно привести к рабочему состоянию четыре экрана.

### SplashScreen

Назначение: решить, куда отправить пользователя при запуске.

UI:

- простой экран с логотипом/названием Swirl;
- loading indicator;
- короткий текст вроде `Загружаем...`;
- если ошибка сети при проверке токена - сообщение и кнопка `Повторить`.

Логика:

1. При открытии экрана вызвать `authProvider.checkAuth()`.
2. Пока идёт проверка, показывать loading.
3. Если user == null, перейти на `/first`.
4. Если user != null, перейти на `/home`.
5. Если ошибка из-за сети/backend, показать retry.

Не делайте тяжёлую логику прямо в `build`. Лучше вызвать проверку через `initState`, `ConsumerStatefulWidget` или аккуратный post-frame callback.

### FirstScreen

Назначение: стартовый экран для неавторизованного пользователя.

UI:

- дружелюбный заголовок;
- короткое описание приложения;
- кнопка `Войти`;
- кнопка `Зарегистрироваться`.

Переходы:

- `Войти` -> `/login`;
- `Зарегистрироваться` -> `/signup`.

Не нужно показывать здесь список секций, уровни или обучение.

### LoginScreen

Назначение: вход по email и паролю.

Поля:

- email;
- password.

UI:

- форма в `SingleChildScrollView`;
- достаточно отступов;
- password скрыт по умолчанию;
- кнопка отправки;
- ссылка/кнопка на регистрацию.

Логика:

1. Пользователь вводит email/password.
2. При нажатии кнопки сначала запускается локальная валидация.
3. Если форма валидна, вызвать `authProvider.login`.
4. Пока идёт запрос, заблокировать кнопку и показать loading.
5. При успехе перейти на `/home`.
6. При ошибке показать понятное сообщение.

### SignUpScreen

Назначение: регистрация нового пользователя.

Поля:

- name;
- email;
- password;
- confirm password;
- avatar selection.

UI:

- форма в `SingleChildScrollView`;
- блок выбора аватара;
- выбранный аватар должен быть визуально выделен;
- кнопка регистрации;
- ссылка/кнопка на login.

Логика:

1. При открытии экрана загрузить аватары через `avatarsProvider`.
2. Пользователь выбирает аватар.
3. Пользователь заполняет поля.
4. При нажатии кнопки запустить локальную валидацию.
5. Если всё валидно, вызвать `authProvider.register`.
6. При успехе перейти на `/home`.
7. При ошибке показать field errors или общий текст.

## 10. Какая нужна валидация форм

Валидация должна быть простой и понятной.

### Login

Email:

- обязателен;
- должен быть похож на email;
- минимально можно проверить наличие `@` и точки после `@`.

Password:

- обязателен;
- пустой пароль отправлять нельзя.

Примеры сообщений:

```text
Введите email
Введите корректный email
Введите пароль
```

### Sign up

Name:

- обязателен;
- не должен состоять только из пробелов.

Email:

- обязателен;
- должен быть похож на email.

Password:

- обязателен;
- желательно минимум 6 символов, если backend это ожидает;
- не усложняйте правила без необходимости.

Confirm password:

- обязателен;
- должен совпадать с password.

Avatar:

- должен быть выбран;
- если хотите выбирать первый аватар автоматически, делайте это только после успешной загрузки списка.

Примеры сообщений:

```text
Введите имя
Введите email
Введите корректный email
Введите пароль
Повторите пароль
Пароли не совпадают
Выберите аватар
```

### Ошибки от backend

Если backend вернул `validation_error` с `details`, покажите ошибки рядом с соответствующими полями, если это удобно.

Например:

```json
{
  "email": ["Email is required"]
}
```

Если field errors быстро сделать сложно, допустимо на Stage 2 показать общий текст над кнопкой. Но лучше хотя бы email/password/name ошибки привязать к полям.

Для неправильного email/password при login показывайте нейтрально:

```text
Неверный email или пароль
```

Не пишите пользователю, что именно неверно: email или password.

## 11. Как должны работать переходы

Маршруты уже описаны в `lib/app/router.dart`.

Для Stage 2 нужны:

```text
/splash
/first
/login
/signup
/home
```

Правила переходов:

```text
нет токена -> /first
токен валиден -> /home
login success -> /home
signup success -> /home
logout -> /first
```

Подробно:

- приложение стартует с `/splash`;
- `/splash` проверяет токен;
- если токена нет, вызывайте `context.go('/first')`;
- если токен есть и `GET /api/auth/me` успешен, вызывайте `context.go('/home')`;
- если `GET /api/auth/me` вернул `401`, удалите токен и откройте `/first`;
- после успешного login используйте `context.go('/home')`;
- после успешной регистрации используйте `context.go('/home')`;
- после logout используйте `context.go('/first')`.

Используйте `context.go`, а не `context.push`, для главных auth-переходов. Так пользователь не вернётся кнопкой Back на login после успешного входа.

## 12. Loading и error состояния

На Stage 2 важно не оставлять пользователя без реакции.

### SplashScreen

Loading:

```text
Загружаем...
```

Error:

```text
Не удалось проверить вход. Проверьте подключение и попробуйте ещё раз.
```

Кнопка:

```text
Повторить
```

### LoginScreen

Loading:

- кнопка disabled;
- на кнопке можно показать `Входим...`;
- или рядом показать маленький spinner.

Error:

```text
Неверный email или пароль
```

или:

```text
Не удалось подключиться к серверу. Попробуйте ещё раз.
```

### SignUpScreen

Loading аватаров:

```text
Загружаем аватары...
```

Error аватаров:

```text
Не удалось загрузить аватары
```

Кнопка:

```text
Повторить
```

Loading регистрации:

```text
Создаём аккаунт...
```

Error регистрации:

```text
Не удалось создать аккаунт
```

Если email уже занят:

```text
Этот email уже используется
```

### 401 Unauthorized

Если любой защищённый запрос получил `401`:

1. удалить токен;
2. очистить auth state;
3. отправить пользователя на `/first` или `/login`;
4. не показывать сырой текст ошибки.

## 13. Как проверять результат вручную

Перед проверкой убедитесь, что backend запущен и Swagger/API доступны.

### Проверка 1. Первый запуск без токена

1. Удалите приложение с эмулятора или очистите данные приложения.
2. Запустите Flutter.
3. Должен открыться `SplashScreen`.
4. После проверки должен открыться `/first`.

Ожидаемый результат: пользователь видит стартовый экран с кнопками входа и регистрации.

### Проверка 2. Загрузка аватаров

1. Перейдите на регистрацию.
2. Дождитесь загрузки аватаров.
3. Проверьте, что картинки отображаются.
4. Выберите один аватар.

Ожидаемый результат: выбранный аватар визуально выделен.

### Проверка 3. Ошибки формы регистрации

1. Нажмите регистрацию с пустыми полями.
2. Введите неправильный email.
3. Введите разные password и confirm password.

Ожидаемый результат: ошибки видны рядом с полями или в понятном общем блоке.

### Проверка 4. Успешная регистрация

1. Введите новое имя.
2. Введите новый email.
3. Введите пароль и подтверждение.
4. Выберите аватар.
5. Нажмите регистрацию.

Ожидаемый результат:

- backend вернул `accessToken`;
- токен сохранился;
- приложение перешло на `/home`.

### Проверка 5. Перезапуск приложения

1. Закройте приложение.
2. Запустите снова.

Ожидаемый результат:

- открывается `SplashScreen`;
- Flutter читает сохранённый токен;
- вызывает `GET /api/auth/me`;
- если токен валиден, открывает `/home`.

### Проверка 6. Login

1. Очистите токен через logout или переустановку приложения.
2. Откройте `/login`.
3. Введите email/password созданного пользователя.
4. Нажмите вход.

Ожидаемый результат: приложение переходит на `/home`.

### Проверка 7. Неверный login

1. Введите правильный email и неправильный пароль.
2. Нажмите вход.

Ожидаемый результат:

- приложение остаётся на login;
- показывает понятную ошибку;
- не сохраняет новый токен.

### Проверка 8. Backend выключен

1. Остановите backend.
2. Запустите приложение или попробуйте login.

Ожидаемый результат:

- приложение не падает;
- показывает ошибку подключения;
- есть возможность повторить.

### Проверка 9. Маленький экран и клавиатура

1. Откройте login/signup на маленьком Android-эмуляторе.
2. Нажмите на поле ввода, чтобы открылась клавиатура.
3. Прокрутите форму.

Ожидаемый результат:

- кнопки и поля доступны;
- нет жёлто-чёрного overflow;
- форма прокручивается.

## 14. Какие команды запускать

Команды запускать из папки Flutter-проекта:

```bash
cd frontend/swirl_app
```

Установить зависимости:

```bash
flutter pub get
```

Проверить код:

```bash
flutter analyze
```

Запустить приложение:

```bash
flutter run
```

Если в проекте есть тесты, дополнительно можно запустить:

```bash
flutter test
```

Для Android emulator backend origin обычно должен быть:

```text
http://10.0.2.2:5000
```

Если backend запущен на другом порту, передайте origin через dart define:

```bash
flutter run --dart-define=SWIRL_BACKEND_ORIGIN=http://10.0.2.2:5000
```

Если backend использует другой порт, замените `5000` на реальный порт.

## 15. Частые ошибки и как их исправить

### Backend не запущен

Симптомы:

- login долго грузится;
- появляется network error;
- аватары не загружаются;
- Splash не может проверить токен.

Что сделать:

1. Запустите backend.
2. Откройте Swagger или проверьте `GET /api/avatars`.
3. Повторите действие во Flutter.

### Неправильный backend URL

Симптомы:

- Flutter отправляет запросы не туда;
- в логах видно connection refused;
- Swagger работает в браузере, но приложение не подключается.

Что проверить:

- `ApiClient.backendOrigin`;
- значение `SWIRL_BACKEND_ORIGIN`;
- порт backend;
- используется ли `http` или `https`.

Для Android emulator обычно нужно:

```text
http://10.0.2.2:5000
```

Не используйте `localhost` внутри Android emulator для доступа к backend на компьютере.

### Android emulator не видит localhost

Проблема:

```text
localhost
```

внутри Android emulator означает сам эмулятор, а не ваш компьютер.

Решение:

```text
10.0.2.2
```

Пример:

```text
http://10.0.2.2:5000/api
```

### 401 после login

Возможные причины:

- токен не сохранился;
- interceptor не добавляет `Authorization`;
- заголовок отправляется без `Bearer`;
- backend URL у login и me разный;
- старый невалидный токен остался в storage;
- системное время сильно отличается.

Что проверить:

1. После login в ответе есть `accessToken`.
2. `tokenStorage.saveAccessToken` реально вызывается.
3. В interceptor заголовок выглядит так:

```text
Authorization: Bearer jwt-token
```

4. `GET /api/auth/me` вызывается после сохранения токена.
5. При необходимости очистите данные приложения и попробуйте снова.

### Аватары не грузятся

Возможные причины:

- backend не запущен;
- `GET /api/avatars` возвращает ошибку;
- в базе нет активных аватаров;
- `imageUrl` относительный, а Flutter пытается открыть его как полный URL;
- media files не раздаются backend-ом.

Что проверить:

1. Откройте `GET /api/avatars` в Swagger.
2. Проверьте, что в ответе есть элементы.
3. Проверьте, что `imageUrl` начинается с `/media`.
4. Соберите полный URL через backend origin.
5. Откройте полный URL картинки в браузере.

Правильно:

```text
http://10.0.2.2:5000/media/avatars/avatar_1.png
```

Неправильно:

```text
/media/avatars/avatar_1.png
```

если этот путь напрямую передать в `Image.network`.

### Overflow на форме

Симптомы:

- жёлто-чёрные полосы;
- кнопка регистрации скрыта клавиатурой;
- поля не помещаются на маленьком экране.

Что сделать:

1. Обернуть содержимое формы в `SingleChildScrollView`.
2. Использовать `SafeArea`.
3. Добавить `padding`.
4. Не задавать слишком большие фиксированные высоты.
5. Проверить экран с открытой клавиатурой.
6. Для длинной формы регистрации разрешить прокрутку.

Пример хорошей идеи для формы:

```text
SafeArea
SingleChildScrollView
Padding
Column
```

### Ошибка парсинга JSON

Симптомы:

- приложение падает после ответа backend;
- в логах ошибка типа `type 'Null' is not a subtype...`;
- модель ждёт одно поле, а backend возвращает другое.

Что сделать:

1. Сравнить модель с `docs/03_API_CONTRACT.md`.
2. Проверить реальные ответы в Swagger.
3. Убедиться, что поля называются camelCase:

```text
accessToken
avatarUrl
imageUrl
```

4. Для nullable полей использовать `String?`.

### Сырые ошибки показываются пользователю

Плохо:

```text
DioException [bad response]: This exception was thrown...
```

Хорошо:

```text
Не удалось подключиться к серверу. Попробуйте ещё раз.
```

Решение:

- ловите `DioException`;
- доставайте `ApiError`, если backend вернул JSON;
- иначе показывайте дружелюбное fallback-сообщение.

## 16. Порядок выполнения Stage 2

Ниже удобный порядок работы.

### Шаг 1. Проверить Stage 1

1. Откройте `frontend/swirl_app`.
2. Запустите:

```bash
flutter pub get
flutter analyze
```

3. Убедитесь, что проект собирается до начала авторизации.

### Шаг 2. Проверить backend URL

1. Запустите backend.
2. Узнайте порт backend.
3. Проверьте, что для Android emulator используется `10.0.2.2`, а не `localhost`.
4. Проверьте `ApiClient.backendOrigin`.

### Шаг 3. Подготовить модели

Создайте или поправьте:

```text
UserModel
AuthResponseModel
AvatarModel
ApiError
```

После этого код экранов не должен вручную доставать поля из JSON.

### Шаг 4. Сделать AuthApi

Создайте `AuthApi` с методами:

```text
getAvatars
login
register
me
```

Проверьте, что пути берутся из `ApiPaths`.

### Шаг 5. Проверить token storage и interceptor

Проверьте:

- токен сохраняется в `flutter_secure_storage`;
- токен читается на старте;
- токен удаляется при logout и `401`;
- Dio добавляет `Authorization: Bearer <token>`.

### Шаг 6. Сделать Riverpod providers

Создайте:

```text
authProvider
avatarsProvider
```

В `authProvider` должны быть методы:

```text
checkAuth
login
register
logout
```

### Шаг 7. Сделать SplashScreen

Сделайте проверку токена и переход:

```text
нет токена -> /first
токен валиден -> /home
токен невалиден -> удалить токен -> /first
ошибка сети -> показать retry
```

### Шаг 8. Сделать FirstScreen

Добавьте две основные кнопки:

```text
Войти -> /login
Зарегистрироваться -> /signup
```

### Шаг 9. Сделать LoginScreen

Добавьте:

- email field;
- password field;
- form validation;
- вызов `authProvider.login`;
- loading на кнопке;
- error message;
- переход на `/home` при успехе.

### Шаг 10. Сделать SignUpScreen

Добавьте:

- name field;
- email field;
- password field;
- confirm password field;
- загрузку аватаров;
- выбор аватара;
- form validation;
- вызов `authProvider.register`;
- loading/error состояния;
- переход на `/home` при успехе.

### Шаг 11. Проверить навигацию

Проверьте все сценарии:

```text
нет токена -> /first
login -> /home
signup -> /home
перезапуск с токеном -> /home
401 -> /first
logout -> /first
```

### Шаг 12. Проверить UI

Проверьте:

- маленький экран;
- открытая клавиатура;
- длинные ошибки;
- пустой список аватаров;
- медленный интернет или выключенный backend.

### Шаг 13. Запустить финальные команды

```bash
flutter pub get
flutter analyze
flutter run
```

Если есть тесты:

```bash
flutter test
```

## 17. Что не делать на Stage 2

Не нужно:

- делать профиль полностью;
- делать смену аватара в профиле;
- делать секции и уровни;
- делать обучение словам;
- делать упражнения;
- делать daily test;
- добавлять refresh token;
- добавлять password reset;
- добавлять email confirmation;
- добавлять user-uploaded avatars;
- добавлять dark theme;
- добавлять новые зависимости без реальной необходимости;
- менять backend.

## 18. Короткий чеклист готовности

Stage 2 можно считать готовым, если:

- [ ] `GET /api/avatars` работает на экране регистрации.
- [ ] Пользователь может выбрать аватар.
- [ ] Login форма валидирует email/password.
- [ ] Sign up форма валидирует name/email/password/confirm password/avatar.
- [ ] `POST /api/auth/login` сохраняет JWT и ведёт на `/home`.
- [ ] `POST /api/auth/register` сохраняет JWT и ведёт на `/home`.
- [ ] `SplashScreen` читает токен.
- [ ] `SplashScreen` вызывает `GET /api/auth/me`, если токен есть.
- [ ] Валидный токен ведёт на `/home`.
- [ ] Отсутствующий или невалидный токен ведёт на `/first`.
- [ ] `401` удаляет токен.
- [ ] Logout-метод удаляет токен и чистит auth state.
- [ ] Формы не дают overflow на маленьком экране.
- [ ] Loading состояния видны.
- [ ] Error состояния понятны.
- [ ] Сырые exception-тексты не показываются пользователю.
- [ ] `flutter analyze` проходит.
- [ ] Приложение запускается через `flutter run`.

