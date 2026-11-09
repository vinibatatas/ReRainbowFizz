# ReRainbowFizz

Utilitário nativo de alto desempenho em C# / .NET 10 desenvolvido para o teclado mecânico 60% **Redragon Fizz Rainbow (K617-R)**.

O modelo Rainbow não possui suporte ao software RGB oficial da fabricante devido a limitações de firmware. Este projeto resolve essa limitação criando uma camada completa de macros e mídia no Windows operada diretamente pela tecla física **FN2** e por **Hold Layer** (Caps Lock), mantendo os atalhos de iluminação do teclado intactos.

---

## 1. Requisitos para Compilação

- **Sistema Operacional:** Windows 10 (Build 19041+) ou Windows 11 (64-bit).
- **SDK:** [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ou superior.
- **Ferramentas Opcionais:** Visual Studio 2026, JetBrains Rider ou Visual Studio Code (com extensão C# Dev Kit).

---

## 2. Instruções de Compilação (Passo a Passo)

Abra o **PowerShell** ou **Terminal** na pasta raiz onde você extraiu ou clonou o projeto:

```powershell
cd ReRainbowFizz
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
O executável final estará pronto em `Publish_Portatil\ReRainbowFizz.exe`.

---

## 3. Compilando o Instalador e o Desinstalador

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

## 4. Atalhos Recomendados de Fábrica

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

## 5. Licença e Créditos

Desenvolvido para a comunidade de usuários do teclado mecânico Redragon Fizz Rainbow.  
Distribuído sob a licença **MIT** (livre para uso, modificação e distribuição).
