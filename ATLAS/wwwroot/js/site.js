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

    // Sino de notificações do dashboard
    const dashBell = document.getElementById("dashBell");
    const dashBellBtn = document.getElementById("dashBellBtn");
    const notifDrop = document.getElementById("notifDrop");
    const dashBellCount = document.getElementById("dashBellCount");

    // Persiste as notificações atuais como lidas (badge zera no banco) e esconde o contador.
    function marcarNotificacoesLidas() {
        if (!notifDrop || !dashBellCount) return;
        const chaves = Array.from(notifDrop.querySelectorAll(".notif-item[data-chave]"))
            .map(function (item) { return item.getAttribute("data-chave"); })
            .filter(Boolean);
        if (!chaves.length) return;

        const tokenMeta = document.querySelector('meta[name="csrf-token"]');
        fetch("/aluno/notificacoes/lidas", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": tokenMeta ? tokenMeta.content : ""
            },
            body: JSON.stringify({ chaves: chaves }),
            credentials: "same-origin"
        }).catch(function () { /* silencioso: o badge volta na próxima carga da página */ });
        dashBellCount.remove();
    }

    if (dashBell && dashBellBtn && notifDrop) {
        var marcouAbertura = false;
        function setNotif(open) {
            notifDrop.hidden = !open;
            dashBellBtn.setAttribute("aria-expanded", String(open));
            if (open && !marcouAbertura) {
                marcouAbertura = true;
                marcarNotificacoesLidas();
            }
        }
        dashBellBtn.addEventListener("click", function (e) {
            e.stopPropagation();
            setNotif(notifDrop.hidden);
        });
        document.addEventListener("click", function (e) {
            if (!dashBell.contains(e.target)) setNotif(false);
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") setNotif(false);
        });
    }

    // Limpeza de estado local ao sair — defensivo, além do cookie e do servidor.
    function limparStorageAtlas() {
        try {
            Object.keys(localStorage).forEach(function (k) { if (k.indexOf("atlas:") === 0) localStorage.removeItem(k); });
            Object.keys(sessionStorage).forEach(function (k) { if (k.indexOf("atlas:") === 0) sessionStorage.removeItem(k); });
        } catch (e) { /* armazenamento indisponível — o logout segue normalmente */ }
    }
    document.querySelectorAll(".sair-form").forEach(function (form) {
        form.addEventListener("submit", limparStorageAtlas);
    });

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

    // Galeria de fotos da página Sobre (modal com navegação e X transparente)
    const galeriaFotos = {
        fachada: ["/images/fachada.jpg"],
        academia: ["/images/academia.jpg", "/images/mais-equipamento.jpg", "/images/mais-foto-dos-equipamentos.jpg"],
        pilates: ["/images/pilates.jpg", "/images/espaco-pilates.jpg"]
    };

    const galeriaModal = document.getElementById("galeriaModal");
    const galeriaSlide = document.getElementById("galeriaSlide");
    const galeriaCounter = document.getElementById("galeriaCounter");
    const galeriaPrev = document.getElementById("galeriaPrev");
    const galeriaNext = document.getElementById("galeriaNext");
    const galeriaFechar = document.getElementById("galeriaFechar");

    if (galeriaModal && galeriaSlide) {
        let fotosAtuais = [];
        let indiceAtual = 0;

        function mostraSlide(i) {
            indiceAtual = (i + fotosAtuais.length) % fotosAtuais.length;
            galeriaSlide.src = fotosAtuais[indiceAtual];
            galeriaSlide.alt = galeriaSlide.alt || "";
            if (galeriaCounter) {
                galeriaCounter.textContent = (indiceAtual + 1) + " / " + fotosAtuais.length;
            }
            if (galeriaPrev) galeriaPrev.hidden = fotosAtuais.length === 1;
            if (galeriaNext) galeriaNext.hidden = fotosAtuais.length === 1;
        }

        function abreGaleria(nome) {
            fotosAtuais = galeriaFotos[nome] || [];
            if (!fotosAtuais.length) return;
            indiceAtual = 0;
            mostraSlide(0);
            galeriaModal.classList.add("aberto");
            galeriaModal.setAttribute("aria-hidden", "false");
            document.body.style.overflow = "hidden";
        }

        function fechaGaleria() {
            galeriaModal.classList.remove("aberto");
            galeriaModal.setAttribute("aria-hidden", "true");
            galeriaSlide.src = "";
            document.body.style.overflow = "";
        }

        document.querySelectorAll(".galeria-item[data-galeria]").forEach(function (item) {
            function abrir() { abreGaleria(item.getAttribute("data-galeria")); }
            item.addEventListener("click", abrir);
            item.addEventListener("keydown", function (e) {
                if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    abrir();
                }
            });
        });

        if (galeriaFechar) galeriaFechar.addEventListener("click", fechaGaleria);
        if (galeriaPrev) galeriaPrev.addEventListener("click", function () { mostraSlide(indiceAtual - 1); });
        if (galeriaNext) galeriaNext.addEventListener("click", function () { mostraSlide(indiceAtual + 1); });
        galeriaModal.addEventListener("click", function (e) {
            if (e.target === galeriaModal) fechaGaleria();
        });
        document.addEventListener("keydown", function (e) {
            if (!galeriaModal.classList.contains("aberto")) return;
            if (e.key === "Escape") fechaGaleria();
            if (e.key === "ArrowLeft") mostraSlide(indiceAtual - 1);
            if (e.key === "ArrowRight") mostraSlide(indiceAtual + 1);
        });
    }
})();
