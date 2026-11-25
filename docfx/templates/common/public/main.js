// Adds a button to the navbar that opens the same page in the other language.
// The site is built per language into <root>/en/ and <root>/ja/ with the same page structure.
const languages = { en: { other: 'ja', title: '日本語' }, ja: { other: 'en', title: 'English' } };
const match = location.pathname.match(/^(.*?\/)(en|ja)\/(.*)$/);

let iconLinks = [];
if (match) {
  const [, root, lang, rest] = match;
  const { other, title } = languages[lang];
  iconLinks = [{ icon: 'translate', href: `${root}${other}/${rest}${location.search}${location.hash}`, title }];

  // Remember the language last viewed so that the root index.html opens it next time.
  try {
    localStorage.setItem('D3Fragments.lang', lang);
  } catch {
    // Storage may be unavailable (private mode, blocked site data).
  }
}

export default { iconLinks };
