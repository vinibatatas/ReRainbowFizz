# ReRainbowFizz — Código-Fonte (.NET 10 / C#)

Utilitário nativo de alto desempenho em C# / .NET 10 desenvolvido para o teclado mecânico 60% **Redragon Fizz Rainbow (K617-R)**.

O modelo Rainbow não possui suporte ao software RGB oficial da fabricante devido a limitações de firmware da sua controladora Sinowealth (VID: `0x258A`, PID: `0x002A`). Este projeto resolve essa limitação criando uma camada completa de macros e mídia no Windows operada diretamente pela tecla física **FN2** e por **Hold Layer** (Caps Lock), mantendo os atalhos de iluminação do teclado intactos.

---

## 1. Arquitetura do Projeto (Clean Architecture)

O código-fonte segue separação estrita de responsabilidades, modularidade e atomização em camadas limpas:

```
Desenvolvimento/
├── Core/
│   ├── Enums/
│   │   └── MacroEnums.cs            # Tipos de ações (Mídia, Atalho, App, Texto) e comandos
│   ├── Models/
│   │   ├── MacroBinding.cs          # Definição e serialização de uma macro
│   │   ├── MacroProfile.cs          # Perfil com dicionário O(1) de mapeamentos
│   │   └── AppConfig.cs             # Configuração global, timeouts, camadas e perfis
│   └── Services/
│       ├── MacroExecutor.cs         # Disparo de atalhos sintéticos (SendInput, processos)
│       └── MacroHookEngine.cs       # Hook de baixo nível (WH_KEYBOARD_LL) e interceptação de FN2
│
├── Infrastructure/
│   ├── Audio/
│   │   └── WindowsAudioService.cs   # Controle nativo de microfone via Windows CoreAudio (WASAPI COM)
│   ├── Native/
│   │   └── NativeMethods.cs         # P/Invoke seguro para user32, kernel32, gdi32 e dwmapi
│   ├── Storage/
│   │   └── ConfigService.cs         # Persistência atômica em JSON em %LocalAppData%\ReRainbowFizz
│   └── SystemIntegration/
│       └── StartupManager.cs        # Registro na chave HKCU Run do Windows para inicialização
│
├── UI/
│   ├── Common/
│   │   └── ApplePalette.cs          # Design System Dark HIG e gerador de retângulos arredondados
│   ├── Components/                  # Componentes reutilizáveis com Double Buffering anti-flicker
│   │   ├── AppleButton.cs           # Botão estilizado com estados Outline, PrimaryRed e Hover
│   │   ├── AppleCard.cs             # Contêiner elegante com traçado sutil de 1px
│   │   ├── AppleSegmentedControl.cs # Seletor de abas e perfis horizontais estilo iPad/macOS
│   │   ├── AppleStepper.cs          # Contador numérico moderno para ajustes de tempo e passos
│   │   └── AppleToggle.cs           # Interruptor tipo pílula estilo iOS/macOS
│   ├── Forms/
│   │   ├── MainForm.cs              # Orquestrador principal da UI, System Tray e eventos
│   │   ├── MacroEditorForm.cs       # Janela modal para gravação e configuração de macros
│   │   └── LayerHudForm.cs          # HUD flutuante não intrusivo com click-through (não rouba foco)
│   └── Keyboard/
│       ├── KeycapDef.cs             # Registro imutável das propriedades físicas de cada tecla
│       ├── FizzAbnt2Layout.cs       # Blueprint exato do layout ABNT2 de 63 teclas do Fizz
│       └── FizzKeyboardControl.cs   # Renderizador vetorial responsivo com detecção de cliques
│
├── Installer/                       # Projeto do instalador automático para Windows
│   ├── ReRainbowFizzSetup.csproj
│   └── SetupForm.cs
│
├── Uninstaller/                     # Projeto do desinstalador limpo do sistema
│   ├── ReRainbowFizzUninstall.csproj
│   └── UninstallForm.cs
│
├── Resources/                       # Recursos visuais (PNG 256x256 e ICO multi-resolução)
├── app.manifest                     # Manifesto de compatibilidade com DPI e Windows 10/11
├── Program.cs                       # Ponto de entrada (STAThread, Mutex de instância única)
└── ReRainbowFizz.csproj             # Arquivo principal de build da solução
```

---

## 2. Requisitos para Compilação

- **Sistema Operacional:** Windows 10 (Build 19041+) ou Windows 11 (64-bit).
- **SDK:** [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ou superior.
- **Ferramentas Opcionais:** Visual Studio 2026, JetBrains Rider ou Visual Studio Code (com extensão C# Dev Kit).

---

## 3. Instruções de Compilação (Passo a Passo)

Abra o **PowerShell** ou **Prompt de Comando** dentro da pasta `Desenvolvimento`:

```powershell
cd C:\Users\Vinibatatas\Documents\ProjetoReMacro\Desenvolvimento
```

### A. Executar em Modo Desenvolvimento (Hot Reload / Teste Rápido)
Para compilar e iniciar o aplicativo diretamente a partir do código:

```powershell
dotnet run
```

### B. Compilar em Modo Release (Binários Otimizados)
Gera os arquivos compilados em `bin\Release\net10.0-windows\`:

```powershell
dotnet build -c Release
```

### C. Publicar Versão Portátil (Executável Único e Autossuficiente)
Gera um único executável `.exe` independente que não exige a instalação prévia do .NET na máquina do usuário final:

```powershell
dotnet publish ReRainbowFizz.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ".\Publish_Portatil"
```
O executável final estará pronto em `Desenvolvimento\Publish_Portatil\ReRainbowFizz.exe`.

---

## 4. Compilando o Instalador e o Desinstalador

O projeto inclui códigos dedicados para gerar um instalador e um desinstalador com a mesma interface visual Apple Dark.

### Compilar o Desinstalador:
```powershell
dotnet publish Uninstaller\ReRainbowFizzUninstall.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ".\Publish_Uninstaller"
```
Gera: `Publish_Uninstaller\Desinstalador_ReRainbowFizz.exe`.

### Compilar o Instalador:
1. Copie o executável principal e o desinstalador para a pasta de empacotamento:
   ```powershell
   mkdir -p Installer\AppBundle
   cp Publish_Portatil\* Installer\AppBundle\
   cp Publish_Uninstaller\Desinstalador_ReRainbowFizz.exe Installer\AppBundle\
   ```
2. Crie o arquivo compactado embutido:
   ```powershell
   Compress-Archive -Path Installer\AppBundle\* -DestinationPath Installer\payload.zip -Force
   ```
3. Compile o instalador autônomo:
   ```powershell
   dotnet publish Installer\ReRainbowFizzSetup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ".\Publish_Setup"
   ```
Gera: `Publish_Setup\Instalador_ReRainbowFizz.exe`.

---

## 5. Atalhos Recomendados de Fábrica

Ao abrir o app pela primeira vez ou clicar no botão **Restaurar Padrão**, as seguintes ações vêm pré-configuradas na camada FN2:

| Tecla Física | Ação Executada | Descrição |
|---|---|---|
| `FN2` + `Q` | Faixa Anterior | Volta a música no Spotify/YouTube |
| `FN2` + `W` | Play / Pause | Alterna reprodução de mídia |
| `FN2` + `E` | Próxima Faixa | Avança para a próxima faixa |
| `FN2` + `R` | Mute de Som | Silencia o volume do Windows |
| `FN2` + `A` | Volume − | Diminui o volume do sistema (-4%) |
| `FN2` + `S` | Parar Mídia | Interrompe o reprodutor ativo |
| `FN2` + `D` | Volume + | Aumenta o volume do sistema (+4%) |
| `FN2` + `F` | Mute de Microfone | Alterna o microfone do Windows via CoreAudio com HUD |
| `FN2` + `G` | Mute do Discord | Envia `Ctrl + Shift + M` globalmente |

> [!TIP]
> **Camada Hold (Caps Lock):** Você também pode disparar qualquer uma dessas ações segurando a tecla Caps Lock e pressionando a letra desejada. Um toque rápido no Caps Lock mantém sua função normal de maiúsculas.

---

## 6. Licença e Créditos

Desenvolvido para a comunidade de usuários do teclado mecânico Redragon Fizz Rainbow.  
Distribuído sob a licença **MIT** (livre para uso, modificação e distribuição).
