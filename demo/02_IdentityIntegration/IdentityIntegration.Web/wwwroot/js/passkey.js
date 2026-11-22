// パスキー（WebAuthn）のブラウザ側の処理。
// サーバーが作ったオプションで navigator.credentials を呼び出し、結果の JSON をフォームで送る。
(() => {
    const antiforgeryToken = form => form.querySelector('input[name="__RequestVerificationToken"]').value;

    const fetchOptions = async (url, form) => {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': antiforgeryToken(form) },
        });
        if (!response.ok) {
            throw new Error(`オプションの取得に失敗しました（${response.status}）`);
        }
        return response.json();
    };

    const submitCredential = (form, credential) => {
        form.querySelector('input[name="credentialJson"]').value = credential ? JSON.stringify(credential.toJSON()) : '';
        form.submit();
    };

    const run = async (button, ceremony) => {
        const form = button.closest('form');
        let credential = null;
        try {
            credential = await ceremony(form);
        } catch (error) {
            // 取り消し（NotAllowedError）も含め、空の結果を送ってサーバー側でエラーを表示する
            console.warn(error);
        }
        submitCredential(form, credential);
    };

    // ログイン（認証）
    document.querySelectorAll('[data-passkey-login]').forEach(button => button.addEventListener('click', () =>
        run(button, async form => {
            const options = await fetchOptions(button.dataset.passkeyLogin, form);
            return navigator.credentials.get({ publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(options) });
        })));

    // 登録
    document.querySelectorAll('[data-passkey-register]').forEach(button => button.addEventListener('click', () =>
        run(button, async form => {
            const options = await fetchOptions(button.dataset.passkeyRegister, form);
            return navigator.credentials.create({ publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(options) });
        })));
})();
