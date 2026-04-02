# Характеристики продукта
**Платформа**: Web (Яндекс игры).

**Жанр**: Головоломка, паззл.

# Стек технологий
[FMOD](https://www.fmod.com) - для решения проблемы со звуком в вебе через импорт библиотек на бутстрап сцене.

[uGUI](https://docs.unity3d.com/Packages/com.unity.ugui@2.5/manual/index.html) - old ui.

[Ui Toolkit](https://docs.unity3d.com/6000.3/Documentation/Manual/UIElements.html) - реализация главного меню

[DoTween](https://dotween.demigiant.com) - анимации кнопок и слов.

[VContainer](https://vcontainer.hadashikick.jp) - DI 

# Архитектура
1. Загружается бутстрап-сцена
2. Подключается DI
3. Включается шина событий
4. Далее системы разделены на модули под архитектуру AsmDef.
5. Вся игра управляется через систему событий.
