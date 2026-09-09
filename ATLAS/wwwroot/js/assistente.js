/* Assistente Atlas — respostas 100% locais (sem APIs externas) */
(function () {
    "use strict";

    var TOPICOS = [
        {
            pergunta: "Quantos dias por semana devo treinar?",
            resposta: "Para a maioria das pessoas, 3 a 5 dias por semana já trazem ótimos resultados. O essencial é manter constância: é melhor treinar 3 dias por semana durante meses do que 6 dias por semana e desistir em um mês. Comece com o mínimo que você consegue sustentar e aumente aos poucos."
        },
        {
            pergunta: "Preciso fazer aquecimento antes do treino?",
            resposta: "Sim. Dedique de 5 a 10 minutos para elevar a temperatura do corpo e preparar articulações: caminhada ou bicicleta leve, mobilidade de ombros e quadril e uma série mais leve do primeiro exercício. O aquecimento reduz o risco de lesões e melhora o desempenho nas séries pesadas."
        },
        {
            pergunta: "Devo alongar antes ou depois do treino?",
            resposta: "Alongamentos estáticos (segurar a posição) funcionam melhor DEPOIS do treino, como parte da volta à calma, segurando cada posição por 20 a 30 segundos. Antes do treino, prefira movimentos dinâmicos e leves, que preparam o corpo sem reduzir a força."
        },
        {
            pergunta: "Quanto tempo de descanso entre as séries?",
            resposta: "Para ganho de força: 2 a 4 minutos entre séries pesadas. Para hipertrofia (ganho de massa): 60 a 120 segundos costuma ser suficiente. Para resistência e condicionamento: 30 a 60 segundos. Seu personal ajusta esses tempos na sua ficha — siga a prescrição."
        },
        {
            pergunta: "Sinto dor muscular no dia seguinte. É normal?",
            resposta: "Sim, essa é a chamada dor tardia (DOMS) — uma resposta natural do músculo ao estímulo novo ou mais intenso. Ela costuma durar de 24 a 72 horas. Movimento leve, hidratação e sono ajudam na recuperação. Dor aguda, localizada em articulação e que limita o movimento não é DOMS: procure um profissional."
        },
        {
            pergunta: "O que comer antes e depois do treino?",
            resposta: "Antes (1 a 2h): carboidrato de fácil digestão com um pouco de proteína, como banana com aveia ou pão integral com ovo. Depois: priorize proteína (carne, frango, peixe, ovos, laticínios) + carboidrato para repor energia. Evite treinar em jejum prolongado sem orientação."
        },
        {
            pergunta: "Quanta água devo beber por dia?",
            resposta: "Uma boa referência é cerca de 35 ml por quilo de peso corporal por dia (ex.: 70 kg ≈ 2,5 L), aumentando nos dias de treino intenso ou calor. Leve sempre uma garrafa para a academia e beba pequenos goles entre as séries. Urina clara é sinal de boa hidratação."
        },
        {
            pergunta: "Posso fazer cardio junto com musculação?",
            resposta: "Pode! Se o foco é ganho de massa, deixe o cardio curto (15–25 min) e de preferência depois da musculação ou em dias alternados. Se o foco é emagrecimento ou condicionamento, 2 a 4 sessões semanais combinadas com a musculação aceleram os resultados."
        },
        {
            pergunta: "Preciso de suplemento para ter resultados?",
            resposta: "Não. Suplemento complementa uma alimentação boa — nunca substitui. Os únicos com bom embasamento para a maioria são whey/proteína (praticidade) e creatina, sempre sob orientação de nutricionista ou médico. Foque primeiro em: dieta consistente, sono e treino bem executado."
        },
        {
            pergunta: "Dormir mal atrapalha meu resultado?",
            resposta: "Muito. Durante o sono profundo ocorre a maior liberação de hormônios ligados à recuperação muscular. Dormir menos de 7 horas regularmente aumenta o cansaço, a fome e o risco de lesão, além de reduzir a força no treino. Priorize 7 a 9 horas por noite."
        }
    ];

    var botao = document.getElementById("assistente-botao");
    var painel = document.getElementById("assistente-painel");
    if (!botao || !painel) return;

    var secaoTopicos = document.getElementById("assistente-topicos");
    var secaoResposta = document.getElementById("assistente-resposta");
    var btnFechar = document.getElementById("assistente-fechar");
    var btnVoltar = document.getElementById("assistente-voltar");
    var tituloResposta = document.getElementById("assistente-pergunta");
    var textoResposta = document.getElementById("assistente-texto");

    function abrir() {
        painel.hidden = false;
        botao.setAttribute("aria-expanded", "true");
        painel.classList.remove("fechando");
        mostrarTopicos();
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

    function mostrarTopicos() {
        secaoResposta.hidden = true;
        secaoTopicos.hidden = false;
    }

    function montarBotoes() {
        TOPICOS.forEach(function (topico, indice) {
            var item = document.createElement("button");
            item.type = "button";
            item.className = "assistente-topico";
            item.textContent = topico.pergunta;
            item.addEventListener("click", function () { mostrarResposta(indice); });
            secaoTopicos.appendChild(item);
        });
    }

    function mostrarResposta(indice) {
        var topico = TOPICOS[indice];
        if (!topico) return;
        tituloResposta.textContent = topico.pergunta;
        textoResposta.textContent = topico.resposta;
        secaoTopicos.hidden = true;
        secaoResposta.hidden = false;
        secaoCorpo();
    }

    function secaoCorpo() {
        var corpo = painel.querySelector(".assistente-corpo");
        if (corpo && !secaoResposta.hidden) { corpo.scrollTop = 0; }
    }

    botao.addEventListener("click", alternar);
    btnFechar.addEventListener("click", fechar);
    btnVoltar.addEventListener("click", mostrarTopicos);
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && !painel.hidden) { fechar(); }
    });

    montarBotoes();
})();
