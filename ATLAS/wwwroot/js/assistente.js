/* Assistente Atlas — conversa livre com IA real via /api/assistente/chat */
(function () {
    "use strict";

    var botao = document.getElementById("assistente-botao");
    var painel = document.getElementById("assistente-painel");
    if (!botao || !painel) return;

    var corpo = document.getElementById("assistente-corpo");
    var mensagens = document.getElementById("assistente-mensagens");
    var sugestoes = document.getElementById("assistente-sugestoes");
    var form = document.getElementById("assistente-form");
    var input = document.getElementById("assistente-input");
    var btnEnviar = document.getElementById("assistente-enviar");
    var btnFechar = document.getElementById("assistente-fechar");

    // Histórico da sessão (fica só nesta aba; nada sensível é enviado).
    var historico = [];
    var processando = false;

    function abrir() {
        painel.hidden = false;
        botao.setAttribute("aria-expanded", "true");
        painel.classList.remove("fechando");
        scrollFinal();
        setTimeout(function () { if (window.innerWidth > 480) input.focus(); }, 80);
    }

    function fechar() {
        painel.classList.add("fechando");
        botao.setAttribute("aria-expanded", "false");
        painel.addEventListener("animationend", function aoTerminar() {
            painel.removeEventListener("animationend", aoTerminar);
            painel.hidden = true;
            painel.classList.remove("fechando");
        });
    }

    function alternar() {
        if (painel.hidden) { abrir(); } else { fechar(); }
    }

    function scrollFinal() {
        if (corpo) { corpo.scrollTop = corpo.scrollHeight; }
    }

    function adicionarMensagem(texto, tipo) {
        var div = document.createElement("div");
        div.className = "msg " + (tipo === "usuario" ? "msg-usuario" : tipo === "erro" ? "msg-erro" : "msg-assistente");
        div.textContent = texto;
        mensagens.appendChild(div);
        scrollFinal();
        return div;
    }

    function adicionarIndicador() {
        var div = document.createElement("div");
        div.className = "msg msg-assistente msg-digitando";
        div.setAttribute("aria-label", "Assistente digitando");
        div.innerHTML = "<span></span><span></span><span></span>";
        mensagens.appendChild(div);
        scrollFinal();
        return div;
    }

    function setProcessando(on) {
        processando = on;
        btnEnviar.disabled = on;
        input.disabled = on;
    }

    function enviar(texto) {
        texto = (texto || "").trim();
        if (!texto || processando) return;

        if (sugestoes) { sugestoes.hidden = true; }
        adicionarMensagem(texto, "usuario");
        historico.push({ papel: "user", texto: texto });
        if (historico.length > 20) { historico = historico.slice(-20); }

        input.value = "";
        setProcessando(true);
        var indicador = adicionarIndicador();

        fetch("/api/assistente/chat", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ mensagem: texto, historico: historico.slice(0, -1) }),
            credentials: "same-origin"
        })
            .then(function (resp) {
                return resp.json().then(function (data) {
                    if (!resp.ok) { throw new Error(data && data.erro ? data.erro : "Não consegui responder agora."); }
                    return data;
                });
            })
            .then(function (data) {
                indicador.remove();
                var resposta = (data && data.resposta) ? data.resposta : "Sem resposta do assistente.";
                adicionarMensagem(resposta, "assistente");
                historico.push({ papel: "assistant", texto: resposta });
            })
            .catch(function (err) {
                indicador.remove();
                adicionarMensagem(err.message || "Não consegui falar com o assistente. Verifique sua conexão e tente de novo.", "erro");
            })
            .finally(function () {
                setProcessando(false);
                input.focus();
            });
    }

    form.addEventListener("submit", function (e) {
        e.preventDefault();
        enviar(input.value);
    });

    if (sugestoes) {
        sugestoes.addEventListener("click", function (e) {
            var btn = e.target.closest(".assistente-chip");
            if (btn) { enviar(btn.textContent); }
        });
    }

    botao.addEventListener("click", alternar);
    btnFechar.addEventListener("click", fechar);
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && !painel.hidden) { fechar(); }
    });
})();
