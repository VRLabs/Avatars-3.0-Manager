using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DreadScripts.Localization
{
	public static class LocalizationConstants
	{
		//This is a prefix preference key for the language of a specific type. 1st in language setting priority.
		internal const string LANGUAGE_KEY_PREFIX = "DSLocalizationLanguage";

		//This is a general preference key for the preferred language. 2nd in language setting priority.
		internal const string PREFERRED_LANGUAGE_KEY = "DSLocalizationPreferredLanguage";

		public static readonly Dictionary<string, string> LanguageWordTranslationDictionary =
			new Dictionary<string, string>()
			{
				{ "བོད་སྐད་", "སྐད་" }, // Tibetan
				{ "ខ្មែរ", "ភាសា" }, // Khmer
				{ "සිංහල", "භාෂාව" }, // Sinhala
				{ "ᏣᎳᎩ", "ᎦᏬᏂᎯᏍᏗ" }, // Cherokee
				{ "አማርኛ", "ቋንቋ" }, // Amharic
				{ "ትግርኛ", "ቋንቋ" }, // Tigrinya
				{ "မြန်မာ", "ဘာသာစကား" }, // Burmese
				{ "Afrikaans", "Taal" }, // Afrikaans
				{ "Anarâškielâ", "Kielâ" }, // Inari Sami
				{ "Azərbaycan", "Dil" }, // Azerbaijani
				{ "Bahasa Melayu", "Bahasa" }, // Malay
				{ "Bosanski", "Jezik" }, // Bosnian
				{ "Brezhoneg", "Yezh" }, // Breton
				{ "Català", "Llengua" }, // Catalan
				{ "Čeština", "Jazyk" }, // Czech
				{ "Cymraeg", "Iaith" }, // Welsh
				{ "Dansk", "Sprog" }, // Danish
				{ "Davvisámegiella", "Giella" }, // Northern Sami
				{ "Deutsch", "Sprache" }, // German
				{ "Dolnoserbšćina", "Rěc" }, // Lower Sorbian
				{ "Èdè Yorùbá", "Èdè" }, // Yoruba
				{ "Eesti", "Keel" }, // Estonian
				{ "English", "Language" }, // English
				{ "Español", "Idioma" }, // Spanish
				{ "Euskara", "Hizkuntza" }, // Basque
				{ "Filipino", "Wika" }, // Filipino
				{ "Føroyskt", "Mál" }, // Faroese
				{ "Français", "Langue" }, // French
				{ "Gaeilge", "Teanga" }, // Irish
				{ "Gàidhlig", "Cànan" }, // Scottish Gaelic
				{ "Galego", "Lingua" }, // Galician
				{ "Hausa", "Harshe" }, // Hausa
				{ "ʻōlelo Hawaiʻi", "ʻŌlelo" }, // Hawaiian
				{ "Hornjoserbšćina", "Rěč" }, // Upper Sorbian
				{ "Hrvatski", "Jezik" }, // Croatian
				{ "Igbo", "Asụsụ" }, // Igbo
				{ "Indonesia", "Bahasa" }, // Indonesian
				{ "Isixhosa", "Ulwimi" }, // Xhosa
				{ "Isizulu", "Ulimi" }, // Zulu
				{ "Íslenska", "Tungumál" }, // Icelandic
				{ "Italiano", "Lingua" }, // Italian
				{ "Kalaallisut", "Oqaatsit" }, // Greenlandic
				{ "Kinyarwanda", "Ururimi" }, // Kinyarwanda
				{ "Kiswahili", "Lugha" }, // Swahili
				{ "Latviešu", "Valoda" }, // Latvian
				{ "Lëtzebuergesch", "Sprooch" }, // Luxembourgish
				{ "Lietuvių", "Kalba" }, // Lithuanian
				{ "Magyar", "Nyelv" }, // Hungarian
				{ "Malti", "Lingwa" }, // Maltese
				{ "Nederlands", "Taal" }, // Dutch
				{ "Norsk", "Språk" }, // Norwegian
				{ "Norsk Bokmål", "Språk" }, // Norwegian Bokmål
				{ "Nynorsk", "Språk" }, // Norwegian Nynorsk
				{ "O'Zbek", "Til" }, // Uzbek
				{ "Oromoo", "Afaan" }, // Oromo
				{ "Polski", "Język" }, // Polish
				{ "Português", "Língua" }, // Portuguese
				{ "Română", "Limbă" }, // Romanian
				{ "Rumantsch", "Lingua" }, // Romansh
				{ "Schwiizertüütsch", "Sprache" }, // Swiss German
				{ "Sesotho", "Puo" }, // Southern Sotho
				{ "Sesotho Sa Leboa", "Polelo" }, // Northern Sotho
				{ "Setswana", "Puo" }, // Tswana
				{ "Shqip", "Gjuhë" }, // Albanian
				{ "Slovenčina", "Jazyk" }, // Slovak
				{ "Slovenščina", "Jezik" }, // Slovenian
				{ "Soomaali", "Luqad" }, // Somali
				{ "Suomi", "Kieli" }, // Finnish
				{ "Svenska", "Språk" }, // Swedish
				{ "Tamaziɣt N Laṭlaṣ", "Tutlayt" }, // Atlas Tamazight
				{ "Tiếng Việt", "Ngôn ngữ" }, // Vietnamese
				{ "Türkçe", "Dil" }, // Turkish
				{ "Türkmençe", "Dil" }, // Turkmen
				{ "West-Frysk", "Taal" }, // Western Frisian
				{ "Xitsonga", "Ririmi" }, // Tsonga
				{ "Ελληνικά", "Γλώσσα" }, // Greek
				{ "Беларуская", "Мова" }, // Belarusian
				{ "Български", "Език" }, // Bulgarian
				{ "Қазақ Тілі", "Тіл" }, // Kazakh
				{ "Кыргызча", "Тил" }, // Kyrgyz
				{ "Македонски", "Јазик" }, // Macedonian
				{ "Монгол", "Хэл" }, // Mongolian
				{ "Русский", "Язык" }, // Russian
				{ "Саха Тыла", "Тыл" }, // Yakut
				{ "Српски", "Језик" }, // Serbian
				{ "Тоҷикӣ", "Забон" }, // Tajik
				{ "Українська", "Мова" }, // Ukrainian
				{ "Հայերեն", "Լեզու" }, // Armenian
				{ "עברית", "שפה" }, // Hebrew
				{ "ئۇيغۇرچە", "تىل" }, // Uyghur
				{ "اردو", "زبان" }, // Urdu
				{ "العربية", "لغة" }, // Arabic
				{ "فارسی", "زبان" }, // Persian
				{ "پښتو", "ژبه" }, // Pashto
				{ "कोंकणी", "भास" }, // Konkani
				{ "नेपाली", "भाषा" }, // Nepali
				{ "मराठी", "भाषा" }, // Marathi
				{ "हिन्दी", "भाषा" }, // Hindi
				{ "অসমীয়া", "ভাষা" }, // Assamese
				{ "বাংলা", "ভাষা" }, // Bengali
				{ "ਪੰਜਾਬੀ", "ਭਾਸ਼ਾ" }, // Punjabi
				{ "ગુજરાતી", "ભાષા" }, // Gujarati
				{ "ଓଡ଼ିଆ", "ଭାଷା" }, // Odia
				{ "தமிழ்", "மொழி" }, // Tamil
				{ "తెలుగు", "భాష" }, // Telugu
				{ "ಕನ್ನಡ", "ಭಾಷೆ" }, // Kannada
				{ "മലയാളം", "ഭാഷ" }, // Malayalam
				{ "ไทย", "ภาษา" }, // Thai
				{ "ລາວ", "ພາສາ" }, // Lao
				{ "ქართული", "ენა" }, // Georgian
				{ "한국어", "언어" }, // Korean
				{ "简体中文", "语言" }, // Chinese-Simplified
				{ "繁體中文", "語言" }, // Chinese-Traditional
				{ "日本語", "言語" } // Japanese
			};
	}
}