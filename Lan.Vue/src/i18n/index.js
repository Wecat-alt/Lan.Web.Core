import { createI18n } from "vue-i18n";
import { changeElementPlusLocale } from "./element-locales.js";
import enLocale from "./locales/en.js";
import zhLocale from "./locales/zh.js";

const messages = {
  zh: zhLocale,
  en: enLocale,
};

const savedLocale = localStorage.getItem("language") || "zh";

export const i18n = createI18n({
  legacy: false,
  locale: savedLocale,
  fallbackLocale: "en",
  messages,
});

export function changeLanguage(locale) {
  i18n.global.locale.value = locale;
  localStorage.setItem("language", locale);

  changeElementPlusLocale(locale);

  return locale;
}

export function getCurrentLanguage() {
  return i18n.global.locale.value;
}
