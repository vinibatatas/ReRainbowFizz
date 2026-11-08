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
