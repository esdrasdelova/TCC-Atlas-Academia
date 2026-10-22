(function () {
    var dadosEl = document.getElementById('treino-form-dados');
    if (!dadosEl) return;

    var dados = JSON.parse(dadosEl.textContent);
    var catalogo = dados.catalogo || [];
    var lista = document.getElementById('lista-exercicios');
    var botaoAdd = document.getElementById('btn-add-exercicio');
    if (!lista || !botaoAdd) return;

    function escapar(texto) {
        return String(texto == null ? '' : texto)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function opcoesExercicio(selecionado) {
        var html = '<option value="" disabled' + (!selecionado ? ' selected' : '') + '>Selecione o exerc\u00edcio</option>';
        var ultimoGrupo = null;
        catalogo.forEach(function (e) {
            if (e.grupo !== ultimoGrupo) {
                if (ultimoGrupo !== null) html += '</optgroup>';
                html += '<optgroup label="' + escapar(e.grupo) + '">';
                ultimoGrupo = e.grupo;
            }
            html += '<option value="' + e.id + '"' + (selecionado === e.id ? ' selected' : '') + '>' + escapar(e.nome) + '</option>';
        });
        if (ultimoGrupo !== null) html += '</optgroup>';
        return html;
    }

    function criarLinha(dadosLinha) {
        dadosLinha = dadosLinha || {};
        var linha = document.createElement('div');
        linha.className = 'treino-linha';
        linha.innerHTML =
            '<div class="treino-linha-cabecalho">' +
                '<span class="exercicio-num"></span>' +
                '<strong class="treino-linha-grupo">&nbsp;</strong>' +
                '<div class="treino-linha-acoes">' +
                    '<button type="button" class="acao-mover acao-subir" title="Mover para cima" aria-label="Mover para cima">' +
                        '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18 15l-6-6-6 6"/></svg></button>' +
                    '<button type="button" class="acao-mover acao-descer" title="Mover para baixo" aria-label="Mover para baixo">' +
                        '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 9l6 6 6-6"/></svg></button>' +
                    '<button type="button" class="acao-mover acao-remover" title="Remover exerc\u00edcio" aria-label="Remover">' +
                        '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 6h18M8 6V4h8v2m-9 0l1 14h8l1-14"/></svg></button>' +
                '</div>' +
            '</div>' +
            '<div class="treino-linha-campos">' +
                '<select data-suffix="ExercicioId" required>' + opcoesExercicio(dadosLinha.exercicioId) + '</select>' +
                '<input type="number" data-suffix="Series" placeholder="S\u00e9ries" min="1" max="10" value="' + (dadosLinha.series || 3) + '" required />' +
                '<input type="text" data-suffix="Repeticoes" placeholder=\'Repeti\u00e7\u00f5es (Ex.: "10-12")\' maxlength="30" value="' + escapar(dadosLinha.repeticoes) + '" required />' +
                '<input type="text" data-suffix="Carga" placeholder="Carga (Ex.: 20 kg)" maxlength="30" value="' + escapar(dadosLinha.carga) + '" />' +
                '<input type="number" data-suffix="DescansoSegundos" placeholder="Descanso (seg)" min="0" max="600" step="15" value="' + (dadosLinha.descanso != null && dadosLinha.descanso !== '' ? dadosLinha.descanso : 60) + '" required />' +
                '<input type="text" data-suffix="Observacoes" placeholder="Observa\u00e7\u00f5es do exerc\u00edcio (opcional)" maxlength="200" value="' + escapar(dadosLinha.obs) + '" />' +
            '</div>';

        linha.querySelector('.acao-remover').addEventListener('click', function () {
            linha.remove();
            atualizar();
        });
        linha.querySelector('.acao-subir').addEventListener('click', function () {
            var anterior = linha.previousElementSibling;
            if (anterior) { lista.insertBefore(linha, anterior); atualizar(); }
        });
        linha.querySelector('.acao-descer').addEventListener('click', function () {
            var seguinte = linha.nextElementSibling;
            if (seguinte) { lista.insertBefore(seguinte, linha); atualizar(); }
        });

        var sel = linha.querySelector('select[data-suffix="ExercicioId"]');
        sel.addEventListener('change', function () {
            atualizarGrupo(linha, sel);
        });
        return linha;
    }

    function atualizarGrupo(linha, sel) {
        var selecionada = sel.options[sel.selectedIndex];
        var optgroup = selecionada ? selecionada.closest('optgroup') : null;
        linha.querySelector('.treino-linha-grupo').textContent =
            (optgroup && sel.value) ? optgroup.label : '\u00A0';
    }

    function atualizar() {
        var linhas = lista.querySelectorAll('.treino-linha');
        linhas.forEach(function (linha, indice) {
            linha.querySelectorAll('[data-suffix]').forEach(function (campo) {
                campo.name = 'Exercicios[' + indice + '].' + campo.dataset.suffix;
            });

            linha.querySelector('.exercicio-num').textContent = String(indice + 1).padStart(2, '0');

            var sel = linha.querySelector('select[data-suffix="ExercicioId"]');
            atualizarGrupo(linha, sel);
        });
    }

    botaoAdd.addEventListener('click', function () {
        lista.appendChild(criarLinha());
        atualizar();
    });

    (dados.exercicios && dados.exercicios.length ? dados.exercicios : [null]).forEach(function (exercicio) {
        lista.appendChild(criarLinha(exercicio));
    });

    atualizar();
})();
