(function () {
    "use strict";

    const navbar = document.getElementById("navbar");
    const hamburger = document.getElementById("hamburger");
    const navLinks = document.getElementById("navLinks");
    const links = navLinks ? Array.from(navLinks.querySelectorAll(".nav-link")) : [];

    // Barra de progresso de leitura e retorno ao topo
    const scrollProgress = document.getElementById("scrollProgress");
    const voltarTopo = document.getElementById("voltarTopo");

    // Navbar com fundo ao rolar
    function onScroll() {
        if (window.scrollY > 24) {
            navbar.classList.add("scrolled");
        } else {
            navbar.classList.remove("scrolled");
        }
        atualizarScrollUI();
    }

    function atualizarScrollUI() {
        const altura = document.documentElement.scrollHeight - window.innerHeight;
        const progresso = altura > 0 ? (window.scrollY / altura) * 100 : 0;
        if (scrollProgress) {
            scrollProgress.style.width = progresso + "%";
        }
        if (voltarTopo) {
            voltarTopo.classList.toggle("visivel", window.scrollY > 600);
        }
    }

    if (voltarTopo) {
        voltarTopo.addEventListener("click", function () {
            window.scrollTo({ top: 0, behavior: "smooth" });
        });
    }

    window.addEventListener("scroll", onScroll, { passive: true });
    window.addEventListener("resize", atualizarScrollUI);
    onScroll();

    // Menu mobile
    function closeMenu() {
        hamburger.classList.remove("open");
        navLinks.classList.remove("open");
        hamburger.setAttribute("aria-expanded", "false");
        hamburger.setAttribute("aria-label", "Abrir menu");
        document.body.style.overflow = "";
    }

    if (hamburger && navLinks) {
        hamburger.addEventListener("click", function () {
            const isOpen = navLinks.classList.toggle("open");
            hamburger.classList.toggle("open", isOpen);
            hamburger.setAttribute("aria-expanded", String(isOpen));
            hamburger.setAttribute("aria-label", isOpen ? "Fechar menu" : "Abrir menu");
            document.body.style.overflow = isOpen ? "hidden" : "";
        });

        links.forEach(function (link) {
            link.addEventListener("click", closeMenu);
        });

        window.addEventListener("resize", function () {
            if (window.innerWidth > 860) closeMenu();
        });
    }

    // Link ativo conforme a URL da página (navegação real entre páginas)
    const caminhoAtual = window.location.pathname.toLowerCase().replace(/\/+$/, "") || "/";

    function marcarAtivo() {
        document.querySelectorAll("a.nav-link, .sidebar-nav a").forEach(function (link) {
            const alvo = (link.dataset.rota || link.getAttribute("href") || "").toLowerCase().replace(/\/+$/, "") || "/";
            if (!alvo.startsWith("/")) return;
            const ativo = caminhoAtual === alvo || (alvo !== "/" && caminhoAtual.startsWith(alvo + "/"));
            link.classList.toggle("active", ativo);
            if (ativo) {
                link.setAttribute("aria-current", "page");
            } else {
                link.removeAttribute("aria-current");
            }
        });
    }
    marcarAtivo();

    // Sidebar das áreas autenticadas (dashboard)
    const sidebar = document.getElementById("sidebar");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarClose = document.getElementById("sidebarClose");
    const sidebarOverlay = document.getElementById("sidebarOverlay");

    function setSidebar(open) {
        if (!sidebar) return;
        sidebar.classList.toggle("open", open);
        if (sidebarOverlay) sidebarOverlay.hidden = !open;
    }

    if (sidebar && sidebarToggle) {
        sidebarToggle.addEventListener("click", function () { setSidebar(!sidebar.classList.contains("open")); });
        sidebarClose?.addEventListener("click", function () { setSidebar(false); });
        sidebarOverlay?.addEventListener("click", function () { setSidebar(false); });
        window.addEventListener("resize", function () {
            if (window.innerWidth > 900) setSidebar(false);
        });
    }

    // Loading states em formulários de exclusão/ação
    document.querySelectorAll("form[onsubmit]").forEach(function (form) {
        form.addEventListener("submit", function () {
            var btn = form.querySelector('button[type="submit"]');
            if (btn && !btn.disabled) {
                btn.disabled = true;
                btn.dataset.originalText = btn.textContent;
                btn.textContent = "Processando\u2026";
            }
        });
    });

    // Loading states em formulários de ação (alternar publicação/status)
    document.querySelectorAll("form").forEach(function (form) {
        if (form.querySelector('button[type="submit"]') && !form.onsubmit && !form.querySelector('input[type="file"]')) {
            form.addEventListener("submit", function () {
                var btn = form.querySelector('button[type="submit"]');
                if (btn && !btn.disabled && !btn.dataset.originalText) {
                    btn.disabled = true;
                    btn.dataset.originalText = btn.textContent;
                    btn.textContent = "Salvando\u2026";
                }
            });
        }
    });

    // Animações de entrada
    const revealEls = document.querySelectorAll(".reveal");
    if ("IntersectionObserver" in window) {
        const revealObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("visible");
                    revealObserver.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12 });

        revealEls.forEach(function (el, i) {
            el.style.transitionDelay = (Math.min(i % 6, 5) * 70) + "ms";
            revealObserver.observe(el);
        });
    } else {
        revealEls.forEach(function (el) { el.classList.add("visible"); });
    }
})();
