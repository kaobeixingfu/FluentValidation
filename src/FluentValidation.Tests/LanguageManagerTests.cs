﻿namespace FluentValidation.Tests {
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Resources;
	using Validators;
	using Xunit;

	public class LanguageManagerTests {
		private ILanguageManager _languages;

		public LanguageManagerTests() {
			_languages = new LanguageManager();
		}

		[Fact]
		public void Gets_translation_for_culture() {
			using (new CultureScope("fr")) {
				var msg = _languages.GetStringForValidator<NotNullValidator>();
				msg.ShouldEqual("'{PropertyName}' ne doit pas avoir la valeur null.");
			}
		}

		[Fact]
		public void Gets_translation_for_specific_culture() {
			using (new CultureScope("zh-CN")) {
				var msg = _languages.GetStringForValidator<NotNullValidator>();
				msg.ShouldEqual("请填写 '{PropertyName}'。");
			}
		}

		[Fact]
		public void Gets_translation_for_croatian_culture()
		{
			using (new CultureScope("hr-HR"))
			{
				var msg = _languages.GetStringForValidator<NotNullValidator>();
				msg.ShouldEqual("Niste upisali '{PropertyName}'");
			}
		}

		[Fact]
		public void Falls_back_to_parent_culture() {
			using (new CultureScope("fr-FR")) {
				var msg = _languages.GetStringForValidator<NotNullValidator>();
				msg.ShouldEqual("'{PropertyName}' ne doit pas avoir la valeur null.");
			}
		}

		[Fact]
		public void Falls_back_to_english_when_culture_not_registered() {
			using (new CultureScope("gu-IN")) {
				var msg = _languages.GetStringForValidator<NotNullValidator>();
				msg.ShouldEqual("'{PropertyName}' must not be empty.");
			}
		}

		[Fact]
		public void Falls_back_to_english_when_translation_missing() {
			// Chinese doesn't have enumvalidator translated
			using (new CultureScope("zh-CN")) {
				var msg = _languages.GetStringForValidator<EnumValidator>();
				msg.ShouldEqual("'{PropertyName}' has a range of values which does not include '{PropertyValue}'.");
			}
		}

		[Fact]
		public void Always_use_specific_language() {
			_languages.Culture = new CultureInfo("fr-FR");
			var msg = _languages.GetStringForValidator<NotNullValidator>();
			msg.ShouldEqual("'{PropertyName}' ne doit pas avoir la valeur null.");
		}

		[Fact]
		public void Always_use_specific_language_with_string_source() {
			ValidatorOptions.LanguageManager.Culture = new CultureInfo("fr-FR");
			var stringSource = new LanguageStringSource(nameof(NotNullValidator));
			var msg = stringSource.GetString(null);
			ValidatorOptions.LanguageManager.Culture = null;

			msg.ShouldEqual("'{PropertyName}' ne doit pas avoir la valeur null.");
		}

		[Fact]
		public void Disables_localization() {
			using (new CultureScope("fr")) {
				_languages.Enabled = false;
				var msg = _languages.GetStringForValidator<NotNullValidator>();
				msg.ShouldEqual("'{PropertyName}' must not be empty.");
			}
		}

		[Fact]
		public void Can_replace_message() {
			using (new CultureScope("en-US")) {
				
				var custom = new CustomLanguageManager();
				var msg = custom.GetStringForValidator<NotNullValidator>();
				msg.ShouldEqual("foo");
			}
		}


		[Fact]
		public void All_localizatons_have_same_parameters_as_English() {

			LanguageManager manager = (LanguageManager)_languages;
			var languages = manager.GetSupportedLanguages();
			var keys = manager.GetSupportedTranslationKeys();
			
			Assert.All(languages, l => Assert.All(keys, k => Check_Parameters_Match(l, k)));
		}

		void Check_Parameters_Match(string languageCode, string translationKey) {
			string referenceMessage = _languages.GetString(translationKey);
			string translatedMessage = _languages.GetString(translationKey, CultureInfo.CreateSpecificCulture(languageCode));
			if (referenceMessage == translatedMessage) return;
			var referenceParameters = ExtractTemplateParameters(referenceMessage);
			var translatedParameters = ExtractTemplateParameters(translatedMessage);
			Assert.False(referenceParameters.Count() != translatedParameters.Count() ||
				referenceParameters.Except(translatedParameters).Any(),
				$"Translation for language {languageCode}, key {translationKey} has parameters {string.Join(",", translatedParameters)}, expected {string.Join(",", referenceParameters)}");
		}

		IEnumerable<string> ExtractTemplateParameters(string message) {
			message = message.Replace("{{", "").Replace("}}", "");
			return message.Split('{').Skip(1).Select(s => s.Split('}').First());
		}

		public class CustomLanguageManager : LanguageManager {
			public CustomLanguageManager() {
				AddTranslation("en", "NotNullValidator", "foo");
			}
		}

	}
}