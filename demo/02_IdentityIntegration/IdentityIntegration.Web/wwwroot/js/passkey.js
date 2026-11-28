// Browser-side processing for passkeys (WebAuthn).
// Calls navigator.credentials with the options created by the server, and posts the resulting JSON with a form.
(() => {
    const antiforgeryToken = form => form.querySelector('input[name="__RequestVerificationToken"]').value;

    const fetchOptions = async (url, form) => {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'RequestVerificationToken': antiforgeryToken(form) },
        });
        if (!response.ok) {
            throw new Error(`Failed to get the options (${response.status})`);
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
            // Including cancellation (NotAllowedError), post an empty result so that the server shows the error
            console.warn(error);
        }
        submitCredential(form, credential);
    };

    // Sign-in (authentication)
    document.querySelectorAll('[data-passkey-login]').forEach(button => button.addEventListener('click', () =>
        run(button, async form => {
            const options = await fetchOptions(button.dataset.passkeyLogin, form);
            return navigator.credentials.get({ publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(options) });
        })));

    // Registration
    document.querySelectorAll('[data-passkey-register]').forEach(button => button.addEventListener('click', () =>
        run(button, async form => {
            const options = await fetchOptions(button.dataset.passkeyRegister, form);
            return navigator.credentials.create({ publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(options) });
        })));
})();
