(() => {
    "use strict";
    // Keep Swagger's normal request/response handling, including all error responses.
    window.tkscSwaggerRequest = async request => {
        const url = new URL(request.url, location.href);
        const method = (request.method || "GET").toUpperCase();
        if (url.origin === location.origin && ["POST", "PUT", "DELETE", "PATCH"].includes(method)
            && (url.pathname.startsWith("/api/auth/") || url.pathname.startsWith("/api/sessions/"))) {
            const csrf = await fetch("/api/auth/csrf", { credentials: "same-origin", cache: "no-store" });
            if (!csrf.ok) throw new Error(`Không lấy được mã bảo vệ (HTTP ${csrf.status}). Thử lại sau.`);
            request.headers = request.headers || {};
            request.headers["X-CSRF-TOKEN"] = (await csrf.json()).token;
            if (url.pathname === "/api/auth/google") request.headers["X-TKSC-Swagger"] = "1";
        }
        return request;
    };
    window.tkscSwaggerResponse = response => {
        const url = new URL(response.url || location.href, location.href);
        if (url.origin === location.origin && url.pathname === "/api/auth/google" && response.status === 200) {
            const body = typeof response.body === "string" ? JSON.parse(response.body) : response.body;
            if (body && body.authorizationUrl) {
                const destination = new URL(body.authorizationUrl);
                if (destination.origin === "https://accounts.google.com") {
                    setTimeout(() => location.assign(destination.href), 0);
                }
            }
        }
        return response;
    };
    // After OAuth, execute the real /me operation so Swagger renders its actual response.
    if (new URLSearchParams(location.search).get("googleLogin") === "complete") {
        history.replaceState(null, "", location.pathname + location.hash);
        let stage = 0;
        const observer = new MutationObserver(showAccountResponse);
        const deadline = setTimeout(() => observer.disconnect(), 15000);
        function showAccountResponse() {
            const operation = document.querySelector("#operations-Auth-get_api_auth_me");
            if (!operation) return;
            if (stage === 0) {
                const expand = operation.querySelector(".opblock-summary-control");
                if (!expand) return;
                stage = 1;
                if (expand.getAttribute("aria-expanded") !== "true") expand.click();
            }
            if (stage === 1) {
                const tryButton = operation.querySelector(".try-out__btn");
                if (!tryButton) return;
                stage = 2;
                tryButton.click();
            }
            if (stage === 2) {
                const execute = operation.querySelector(".execute");
                if (!execute || execute.disabled) return;
                stage = 3;
                observer.disconnect();
                clearTimeout(deadline);
                execute.click();
                operation.scrollIntoView({ block: "start" });
            }
        }
        observer.observe(document.documentElement, { childList: true, subtree: true });
        showAccountResponse();
    }
})();
