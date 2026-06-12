using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using UnityEngine;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Servir la página de inicio
app.MapGet("/", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(GetHtmlContent());
});

// Endpoint de simulación
app.MapGet("/api/simulate", (int heuristic = 3, int aiColor = 1, int depth = 4, string opponent = "random") =>
{
    BoardManager bm = new BoardManager();
    Player aiPlayer = new Player();
    aiPlayer.turn = aiColor;
    aiPlayer.maxDepth = depth;
    aiPlayer.heuristic = heuristic;

    // Inyectar boardManager mediante reflexión
    var field = typeof(Player).GetField("boardManager", BindingFlags.NonPublic | BindingFlags.Instance);
    if (field != null)
    {
        field.SetValue(aiPlayer, bm);
    }

    // Inicializar tablero
    Tile[] board = new Tile[64];
    for (int i = 0; i < 64; i++)
    {
        board[i] = new Tile { numTile = i, fila = i / 8, columna = i % 8 };
    }

    // Colocar fichas iniciales
    board[3 * 8 + 3].value = -1; // Blanco
    board[4 * 8 + 4].value = -1; // Blanco
    board[3 * 8 + 4].value = 1;  // Negro
    board[4 * 8 + 3].value = 1;  // Negro

    int currentTurn = 1; // Negro empieza siempre
    bool passBlack = false;
    bool passWhite = false;
    Random rnd = new Random();

    var history = new List<object>();

    // Guardar estado inicial
    history.Add(new
    {
        board = GetBoardArray(board),
        turn = 0,
        move = -1,
        msg = "Estado inicial"
    });

    int safetyLimit = 200;
    while (safetyLimit-- > 0)
    {
        int blackPieces = bm.CountPieces(board, 1);
        int whitePieces = bm.CountPieces(board, -1);

        if (blackPieces + whitePieces == 64 || blackPieces == 0 || whitePieces == 0 || (passBlack && passWhite))
        {
            break;
        }

        var selectable = bm.FindSelectableTiles(board, currentTurn);
        if (selectable.Count == 0)
        {
            if (currentTurn == 1) passBlack = true;
            else passWhite = true;

            history.Add(new
            {
                board = GetBoardArray(board),
                turn = currentTurn,
                move = -1,
                msg = $"El jugador {(currentTurn == 1 ? "Negro" : "Blanco")} no tiene movimientos válidos. Pasa turno."
            });
        }
        else
        {
            if (currentTurn == 1) passBlack = false;
            else passWhite = false;

            int selectedTile = -1;
            string playType = "";
            double utilityCalculated = 0;

            if (currentTurn == aiColor)
            {
                selectedTile = aiPlayer.SelectTile(board);
                playType = $"IA (H{heuristic})";
            }
            else
            {
                if (opponent == "random")
                {
                    selectedTile = selectable[rnd.Next(selectable.Count)];
                    playType = "Oponente (Azar)";
                }
                else
                {
                    // Si fuese IA vs IA
                    Player oppPlayer = new Player { turn = -aiColor, maxDepth = depth, heuristic = 1 };
                    field?.SetValue(oppPlayer, bm);
                    selectedTile = oppPlayer.SelectTile(board);
                    playType = "Oponente (IA H1)";
                }
            }

            if (selectedTile != -1 && selectable.Contains(selectedTile))
            {
                bm.Move(board, selectedTile, currentTurn);
                history.Add(new
                {
                    board = GetBoardArray(board),
                    turn = currentTurn,
                    move = selectedTile,
                    msg = $"{playType} mueve a la casilla {selectedTile}"
                });
            }
            else
            {
                bm.Move(board, selectable[0], currentTurn);
                history.Add(new
                {
                    board = GetBoardArray(board),
                    turn = currentTurn,
                    move = selectable[0],
                    msg = $"{playType} hace movimiento por defecto a {selectable[0]}"
                });
            }
        }

        currentTurn = -currentTurn;
    }

    int finalBlack = bm.CountPieces(board, 1);
    int finalWhite = bm.CountPieces(board, -1);
    int winner = 0;
    if (finalBlack > finalWhite) winner = 1;
    else if (finalWhite > finalBlack) winner = -1;

    return Results.Json(new
    {
        history,
        finalBlack,
        finalWhite,
        winner,
        aiColor,
        heuristic
    });
});

app.Run("http://localhost:5080");

int[] GetBoardArray(Tile[] board)
{
    int[] arr = new int[64];
    for (int i = 0; i < 64; i++)
    {
        arr[i] = board[i].value;
    }
    return arr;
}

string GetHtmlContent()
{
    return @"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Visualizador Othello AI</title>
    <link href=""https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;600;800&display=swap"" rel=""stylesheet"">
    <style>
        :root {
            --bg-color: #0d1117;
            --card-bg: rgba(22, 27, 34, 0.7);
            --border-color: rgba(48, 54, 61, 0.6);
            --primary: #58a6ff;
            --primary-hover: #1f6feb;
            --accent: #2ea44f;
            --text-color: #c9d1d9;
            --text-bold: #f0f6fc;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body {
            font-family: 'Outfit', sans-serif;
            background-color: var(--bg-color);
            color: var(--text-color);
            display: flex;
            flex-direction: column;
            min-height: 100vh;
            align-items: center;
            justify-content: center;
            overflow-x: hidden;
            background-image: radial-gradient(circle at 50% 10%, #161b22 0%, #0d1117 100%);
        }

        header {
            margin: 20px 0;
            text-align: center;
        }

        header h1 {
            font-size: 2.5rem;
            font-weight: 800;
            background: linear-gradient(90deg, #58a6ff, #bc8cff);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            margin-bottom: 5px;
        }

        header p {
            color: #8b949e;
            font-size: 1.1rem;
        }

        .container {
            display: flex;
            flex-direction: row;
            gap: 30px;
            max-width: 1200px;
            width: 95%;
            margin-bottom: 40px;
        }

        @media (max-width: 900px) {
            .container {
                flex-direction: column;
                align-items: center;
            }
        }

        .panel-controls {
            flex: 1;
            background: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 16px;
            padding: 24px;
            backdrop-filter: blur(12px);
            display: flex;
            flex-direction: column;
            gap: 20px;
            height: fit-content;
        }

        .panel-board {
            flex: 1.5;
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 20px;
        }

        .control-group {
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        label {
            font-size: 0.95rem;
            font-weight: 600;
            color: var(--text-bold);
        }

        select, input {
            background-color: #0d1117;
            border: 1px solid var(--border-color);
            color: var(--text-color);
            border-radius: 8px;
            padding: 10px 12px;
            font-family: inherit;
            font-size: 1rem;
            outline: none;
            transition: border-color 0.2s;
        }

        select:focus, input:focus {
            border-color: var(--primary);
        }

        .btn {
            background-color: var(--primary);
            color: #ffffff;
            border: none;
            border-radius: 8px;
            padding: 12px 20px;
            font-size: 1rem;
            font-weight: 600;
            cursor: pointer;
            transition: background-color 0.2s, transform 0.1s;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
        }

        .btn:hover {
            background-color: var(--primary-hover);
        }

        .btn:active {
            transform: scale(0.98);
        }

        .btn-success {
            background-color: var(--accent);
        }

        .btn-success:hover {
            background-color: #2c974b;
        }

        /* Tablero de Othello */
        .board {
            width: 480px;
            height: 480px;
            background-color: #1e592f;
            border: 10px solid #164624;
            border-radius: 12px;
            display: grid;
            grid-template-columns: repeat(8, 1fr);
            grid-template-rows: repeat(8, 1fr);
            box-shadow: 0 10px 30px rgba(0,0,0,0.5);
            overflow: hidden;
        }

        @media (max-width: 520px) {
            .board {
                width: 320px;
                height: 320px;
                border-width: 6px;
            }
        }

        .cell {
            border: 1px solid #164624;
            display: flex;
            align-items: center;
            justify-content: center;
            position: relative;
            cursor: pointer;
            transition: background-color 0.15s;
        }

        .cell:hover {
            background-color: rgba(255, 255, 255, 0.05);
        }

        .disc {
            width: 80%;
            height: 80%;
            border-radius: 50%;
            transition: transform 0.4s ease, background-color 0.4s;
            box-shadow: inset -3px -3px 6px rgba(0,0,0,0.4), 2px 2px 4px rgba(0,0,0,0.3);
        }

        .disc.black {
            background-color: #111;
            transform: rotateY(0deg);
        }

        .disc.white {
            background-color: #f0f0f0;
            transform: rotateY(180deg);
        }

        .disc.empty {
            background-color: transparent;
            box-shadow: none;
        }

        .cell-highlight {
            width: 10px;
            height: 10px;
            background-color: rgba(255, 255, 255, 0.4);
            border-radius: 50%;
            position: absolute;
        }

        .marker-last-move {
            width: 14px;
            height: 14px;
            background-color: #ff5353;
            border-radius: 50%;
            position: absolute;
            z-index: 10;
            border: 2px solid white;
            animation: pulse 1.5s infinite;
        }

        @keyframes pulse {
            0% { transform: scale(0.9); opacity: 0.8; }
            50% { transform: scale(1.2); opacity: 1; }
            100% { transform: scale(0.9); opacity: 0.8; }
        }

        /* Controles de reproducción */
        .playback-controls {
            display: flex;
            gap: 10px;
            width: 100%;
            justify-content: center;
            margin-top: 10px;
        }

        .progress-bar {
            width: 100%;
            display: flex;
            align-items: center;
            gap: 15px;
            margin-top: 10px;
        }

        .progress-bar input[type=range] {
            flex: 1;
            cursor: pointer;
        }

        /* Historial y logs */
        .history-log {
            background-color: #0d1117;
            border: 1px solid var(--border-color);
            border-radius: 8px;
            padding: 15px;
            height: 200px;
            overflow-y: auto;
            font-family: monospace;
            font-size: 0.9rem;
            color: #8b949e;
            display: flex;
            flex-direction: column;
            gap: 8px;
        }

        .history-item {
            border-left: 3px solid var(--primary);
            padding-left: 8px;
            margin-bottom: 4px;
        }

        .history-item.active {
            color: var(--text-bold);
            border-left-color: var(--accent);
            background-color: rgba(46, 164, 79, 0.1);
        }

        /* Marcador */
        .scoreboard {
            display: flex;
            gap: 20px;
            width: 100%;
            justify-content: space-around;
            background: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 12px;
            padding: 15px;
            margin-bottom: 10px;
        }

        .score-box {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 5px;
        }

        .score-box .circle {
            width: 30px;
            height: 30px;
            border-radius: 50%;
            border: 1px solid var(--border-color);
        }

        .score-box .circle.black { background-color: #111; }
        .score-box .circle.white { background-color: #f0f0f0; }

        .score-value {
            font-size: 1.8rem;
            font-weight: 800;
            color: var(--text-bold);
        }

        .heuristics-desc {
            background-color: rgba(88, 166, 255, 0.05);
            border: 1px dashed var(--primary);
            border-radius: 8px;
            padding: 12px;
            font-size: 0.88rem;
            line-height: 1.4;
        }
    </style>
</head>
<body>
    <header>
        <h1>Simulador Othello</h1>
        <p>Entorno de pruebas de heurísticas</p>
    </header>

    <div class=""container"">
        <!-- Controles de Simulación -->
        <div class=""panel-controls"">
            <h2>Simulación</h2>
            
            <div class=""control-group"">
                <label for=""heuristic-select"">Seleccionar Heurística de la IA</label>
                <select id=""heuristic-select"">
                    <option value=""1"">H1: Diferencia básica de fichas</option>
                    <option value=""2"">H2: Movilidad + Pesos de Posición</option>
                    <option value=""3"" selected>H3: Avanzada (Estabilidad + Esquinas)</option>
                    <option value=""4"">H4: Adaptativa por Fases del juego</option>
                </select>
            </div>

            <div class=""control-group"">
                <label for=""ai-color-select"">Color de la IA</label>
                <select id=""ai-color-select"">
                    <option value=""1"" selected>Negras (Juega Primero)</option>
                    <option value=""-1"">Blancas</option>
                </select>
            </div>

            <div class=""control-group"">
                <label for=""opponent-select"">Oponente</label>
                <select id=""opponent-select"">
                    <option value=""random"">Movimientos al Azar</option>
                    <option value=""ia-h1"">IA con Heurística H1 (Básica)</option>
                </select>
            </div>

            <button class=""btn btn-success"" id=""btn-simulate"">
                Iniciar Simulación
            </button>

            <div class=""heuristics-desc"" id=""heuristics-desc"">
                <strong>Heurística actual:</strong> H3 (Estabilidad y penalizaciones dinámicas en casillas C/X).
            </div>

            <hr style=""border-color: var(--border-color);"">

            <h3>Historial de Partida</h3>
            <div class=""history-log"" id=""history-log"">
                <div style=""color: #8b949e;"">Haz clic en 'Iniciar Simulación' para generar una partida.</div>
            </div>
        </div>

        <!-- Tablero y Playback -->
        <div class=""panel-board"">
            <div class=""scoreboard"">
                <div class=""score-box"">
                    <div class=""circle black""></div>
                    <div>Negras (Negras)</div>
                    <div class=""score-value"" id=""score-black"">2</div>
                </div>
                <div class=""score-box"">
                    <div class=""circle white""></div>
                    <div>Blancas (Blancas)</div>
                    <div class=""score-value"" id=""score-white"">2</div>
                </div>
            </div>

            <div class=""board"" id=""board""></div>

            <div class=""progress-bar"">
                <span id=""current-step-label"">Paso: 0/0</span>
                <input type=""range"" id=""progress-slider"" min=""0"" max=""0"" value=""0"" step=""1"">
            </div>

            <div class=""playback-controls"">
                <button class=""btn"" id=""btn-prev"">Anterior</button>
                <button class=""btn"" id=""btn-play"">Reproducir</button>
                <button class=""btn"" id=""btn-next"">Siguiente</button>
            </div>
        </div>
    </div>

    <script>
        let gameData = null;
        let currentStep = 0;
        let isPlaying = false;
        let playInterval = null;

        const boardEl = document.getElementById('board');
        const scoreBlackEl = document.getElementById('score-black');
        const scoreWhiteEl = document.getElementById('score-white');
        const sliderEl = document.getElementById('progress-slider');
        const stepLabelEl = document.getElementById('current-step-label');
        const logEl = document.getElementById('history-log');

        const descriptions = {
            1: ""<strong>H1:</strong> Diferencia directa de fichas."",
            2: ""<strong>H2:</strong> Tabla de pesos posicionales y movilidad normalizada."",
            3: ""<strong>H3:</strong> Estabilidad de piezas y penalizaciones dinámicas en casillas C/X."",
            4: ""<strong>H4:</strong> Comportamiento adaptativo según la fase de la partida.""
        };

        document.getElementById('heuristic-select').addEventListener('change', (e) => {
            document.getElementById('heuristics-desc').innerHTML = descriptions[e.target.value];
        });

        // Crear las casillas inicialmente
        function createBoard() {
            boardEl.innerHTML = '';
            for (let i = 0; i < 64; i++) {
                const cell = document.createElement('div');
                cell.className = 'cell';
                cell.dataset.index = i;
                
                const disc = document.createElement('div');
                disc.className = 'disc empty';
                cell.appendChild(disc);

                // Puntos de referencia en tableros de Othello
                if ([18, 21, 42, 45].includes(i)) {
                    const highlight = document.createElement('div');
                    highlight.className = 'cell-highlight';
                    cell.appendChild(highlight);
                }

                boardEl.appendChild(cell);
            }
        }

        // Dibujar el estado en un paso concreto
        function renderStep(stepIndex) {
            if (!gameData || !gameData.history[stepIndex]) return;

            const step = gameData.history[stepIndex];
            const board = step.board;

            // Actualizar fichas del tablero
            const cells = boardEl.querySelectorAll('.cell');
            cells.forEach((cell, idx) => {
                const disc = cell.querySelector('.disc');
                // Quitar marcas previas de último movimiento
                const mark = cell.querySelector('.marker-last-move');
                if (mark) mark.remove();

                if (board[idx] === 1) {
                    disc.className = 'disc black';
                } else if (board[idx] === -1) {
                    disc.className = 'disc white';
                } else {
                    disc.className = 'disc empty';
                }
            });

            // Colocar marca en el último movimiento
            if (step.move !== -1) {
                const targetCell = cells[step.move];
                const marker = document.createElement('div');
                marker.className = 'marker-last-move';
                targetCell.appendChild(marker);
            }

            // Contar fichas
            let blackCount = 0;
            let whiteCount = 0;
            board.forEach(val => {
                if (val === 1) blackCount++;
                if (val === -1) whiteCount++;
            });

            scoreBlackEl.innerText = blackCount;
            scoreWhiteEl.innerText = whiteCount;

            // Actualizar slider
            sliderEl.value = stepIndex;
            stepLabelEl.innerText = `Paso: ${stepIndex}/${gameData.history.length - 1}`;

            // Resaltar en el log
            const logItems = logEl.querySelectorAll('.history-item');
            logItems.forEach((item, idx) => {
                if (idx === stepIndex) {
                    item.classList.add('active');
                    item.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
                } else {
                    item.classList.remove('active');
                }
            });
        }

        // Llamar API de Simulación
        async function runSimulation() {
            const h = document.getElementById('heuristic-select').value;
            const color = document.getElementById('ai-color-select').value;
            const opp = document.getElementById('opponent-select').value;

            document.getElementById('btn-simulate').innerText = 'Simulando...';
            document.getElementById('btn-simulate').disabled = true;

            try {
                const response = await fetch(`/api/simulate?heuristic=${h}&aiColor=${color}&opponent=${opp}`);
                gameData = await response.json();
                
                currentStep = 0;
                sliderEl.max = gameData.history.length - 1;
                
                // Rellenar Log de la barra lateral
                logEl.innerHTML = '';
                gameData.history.forEach((step, idx) => {
                    const item = document.createElement('div');
                    item.className = 'history-item';
                    item.innerText = `[Paso ${idx}] ${step.msg}`;
                    item.addEventListener('click', () => {
                        currentStep = idx;
                        renderStep(currentStep);
                        pause();
                    });
                    logEl.appendChild(item);
                });

                renderStep(0);
            } catch (err) {
                alert('Error al simular la partida.');
                console.error(err);
            } finally {
                document.getElementById('btn-simulate').innerText = 'Iniciar Simulación';
                document.getElementById('btn-simulate').disabled = false;
            }
        }

        // Reproductor
        function play() {
            if (isPlaying) return;
            isPlaying = true;
            document.getElementById('btn-play').innerText = '❚❚ Pausar';
            playInterval = setInterval(() => {
                if (currentStep < gameData.history.length - 1) {
                    currentStep++;
                    renderStep(currentStep);
                } else {
                    pause();
                }
            }, 600);
        }

        function pause() {
            isPlaying = false;
            document.getElementById('btn-play').innerText = '▶ Reproducir';
            clearInterval(playInterval);
        }

        document.getElementById('btn-simulate').addEventListener('click', runSimulation);

        document.getElementById('btn-play').addEventListener('click', () => {
            if (!gameData) return;
            if (isPlaying) pause();
            else play();
        });

        document.getElementById('btn-prev').addEventListener('click', () => {
            if (!gameData || currentStep <= 0) return;
            pause();
            currentStep--;
            renderStep(currentStep);
        });

        document.getElementById('btn-next').addEventListener('click', () => {
            if (!gameData || currentStep >= gameData.history.length - 1) return;
            pause();
            currentStep++;
            renderStep(currentStep);
        });

        sliderEl.addEventListener('input', (e) => {
            if (!gameData) return;
            pause();
            currentStep = parseInt(e.target.value);
            renderStep(currentStep);
        });

        // Inicio
        createBoard();
    </script>
</body>
</html>";
}
