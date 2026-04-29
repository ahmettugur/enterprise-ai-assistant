import { Injectable } from '@angular/core';

export interface RenderOptions {
  cleanLineBreaks?: boolean;
  allowHtml?: boolean;
}

/**
 * MarkdownRenderer Service
 * markdown-renderer.js'nin Angular karşılığı
 */
@Injectable({
  providedIn: 'root'
})
export class MarkdownRendererService {

  /**
   * Renders markdown-like text to HTML
   */
  render(text: string, options: RenderOptions = {}): string {
    if (!text) return '';

    const cleanLineBreaks = options.cleanLineBreaks !== false; // Default true
    const allowHtml = options.allowHtml || false;

    let rendered = text;

    // Extract and process GRAFİK_HTML blocks BEFORE escaping
    const graphicBlocks: string[] = [];

    // Pattern 1: GRAFİK_HTML with backticks - ```html or ```
    rendered = rendered.replace(/GRAF[İI]K_HTML(?:_\d+)?:\s*```(?:html)?\s*([\s\S]*?)```/gi, (match, htmlContent) => {
      const placeholder = '___GRAPHIC_BLOCK_' + graphicBlocks.length + '___';
      graphicBlocks.push(htmlContent.trim());
      return placeholder;
    });

    // Pattern 2: GRAFİK_HTML without backticks - directly followed by HTML
    rendered = rendered.replace(/GRAF[İI]K_HTML(?:_\d+)?:\s*(<[\s\S]*?<\/script>)/gi, (match, htmlContent) => {
      const placeholder = '___GRAPHIC_BLOCK_' + graphicBlocks.length + '___';
      graphicBlocks.push(htmlContent.trim());
      return placeholder;
    });

    // Pattern 3: Standalone ```html blocks with chart scripts
    rendered = rendered.replace(/```html\s*([\s\S]*?<\/script>\s*)```/gi, (match, htmlContent) => {
      if (htmlContent.includes('ApexCharts') || htmlContent.includes('Chart(') || 
          htmlContent.includes('chart.render') || htmlContent.includes('am5.ready') || 
          htmlContent.includes('amCharts')) {
        const placeholder = '___GRAPHIC_BLOCK_' + graphicBlocks.length + '___';
        graphicBlocks.push(htmlContent.trim());
        return placeholder;
      }
      return match;
    });

    // Pattern 4: Response starts with "html" followed by chart code
    rendered = rendered.replace(/^html\s*(<div[\s\S]*?<\/script>\s*)$/gim, (match, htmlContent) => {
      if (htmlContent.includes('ApexCharts') || htmlContent.includes('Chart(') || 
          htmlContent.includes('chart.render') || htmlContent.includes('am5.ready') || 
          htmlContent.includes('amCharts')) {
        const placeholder = '___GRAPHIC_BLOCK_' + graphicBlocks.length + '___';
        graphicBlocks.push(htmlContent.trim());
        return placeholder;
      }
      return match;
    });

    // Escape HTML if not allowed
    if (!allowHtml) {
      rendered = this.escapeHtml(rendered);
    }

    // Headers
    rendered = rendered.replace(/### (.*?)$/gm, '<h5 class="fw-bold text-primary mt-4 mb-2">$1</h5>');
    rendered = rendered.replace(/## (.*?)$/gm, '<h4 class="fw-bold text-primary mt-4 mb-3">$1</h4>');
    rendered = rendered.replace(/# (.*?)$/gm, '<h3 class="fw-bold text-primary mt-4 mb-3">$1</h3>');

    // Bold and Italic
    rendered = rendered.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    rendered = rendered.replace(/\*(.*?)\*/g, '<em>$1</em>');

    // Code blocks - styled div instead of pre/code to prevent scroll
    rendered = rendered.replace(/```([\s\S]*?)```/g, '<div class="bg-light-primary p-3 rounded my-2" style="white-space: pre-wrap; word-wrap: break-word; overflow-wrap: break-word;">$1</div>');
    // Inline code - simple span styling
    rendered = rendered.replace(/`(.*?)`/g, '<span class="fw-semibold text-primary">$1</span>');

    // Lists
    rendered = rendered.replace(/^\d+\.\s+(.*?)$/gm, '<div class="mb-2">• $1</div>');
    rendered = rendered.replace(/^-\s+(.*?)$/gm, '<div class="mb-1 ms-3">◦ $1</div>');
    rendered = rendered.replace(/^\*\s+(.*?)$/gm, '<div class="mb-1 ms-3">◦ $1</div>');

    // Links
    rendered = rendered.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" target="_blank" class="text-primary">$1</a>');

    // Tables - Handle markdown tables with | syntax
    rendered = this.renderTables(rendered);

    // Clean line breaks if enabled
    if (cleanLineBreaks) {
      rendered = this.cleanLineBreaks(rendered);
    } else {
      // Standard line break handling
      rendered = rendered.replace(/<br\s*\/?>\s*<br\s*\/?>/gi, ' ');
      rendered = rendered.replace(/\n\n/g, ' ');
      rendered = rendered.replace(/\n/g, ' ');
      rendered = rendered.replace(/\s{2,}/g, ' ');
    }

    // Restore graphic HTML blocks with wrapper and unique canvas IDs
    const timestamp = Date.now();
    for (let i = 0; i < graphicBlocks.length; i++) {
      const placeholder = '___GRAPHIC_BLOCK_' + i + '___';
      const uniqueId = 'grafik_' + timestamp + '_' + i;

      let htmlContent = graphicBlocks[i];

      // Find the div id in this block and replace it with unique ID
      const divIdMatch = htmlContent.match(/<div[^>]*id=["']([^"']+)["']/i);
      const originalId = divIdMatch ? divIdMatch[1] : 'anaGrafik';

      // Replace div id with unique ID
      htmlContent = htmlContent.replace(
        new RegExp('id=["\']' + originalId + '["\']', 'gi'),
        'id="' + uniqueId + '"'
      );

      // Replace getElementById references
      htmlContent = htmlContent.replace(
        new RegExp('getElementById\\s*\\(\\s*[\'"]' + originalId + '[\'"]\\s*\\)', 'gi'),
        'getElementById("' + uniqueId + '")'
      );

      // Replace querySelector references
      htmlContent = htmlContent.replace(
        new RegExp('querySelector\\s*\\(\\s*[\'"]#' + originalId + '[\'"]\\s*\\)', 'gi'),
        'querySelector("#' + uniqueId + '")'
      );

      // Replace amCharts am5.Root.new("id") references
      htmlContent = htmlContent.replace(
        new RegExp('am5\\.Root\\.new\\s*\\(\\s*["\']' + originalId + '["\']\\s*\\)', 'gi'),
        'am5.Root.new("' + uniqueId + '")'
      );

      // Check if this is a pie/donut chart and limit size
      const isPieOrDonut = /type:\s*['"](?:pie|doughnut|donut)['"]/.test(htmlContent);
      let containerClass = 'graphic-html-container mt-4 mb-4';
      let containerStyle = '';

      if (isPieOrDonut) {
        containerStyle = ' style="max-width: 400px; margin-left: auto; margin-right: auto;"';
      }

      const wrappedHtml = '<div class="' + containerClass + '"' + containerStyle + ' data-graphic-index="' + i + '" data-graphic-id="' + uniqueId + '">' +
        htmlContent +
        '</div>';
      rendered = rendered.replace(placeholder, wrappedHtml);
    }

    return rendered;
  }

  /**
   * Renders text specifically for AI analysis with predefined styling
   */
  renderAnalysis(text: string): string {
    return this.render(text, {
      cleanLineBreaks: true,
      allowHtml: true
    });
  }

  /**
   * Renders text for chat messages with basic formatting
   */
  renderMessage(text: string): string {
    return this.render(text, {
      cleanLineBreaks: false,
      allowHtml: true
    });
  }

  /**
   * Cleans excessive line breaks to prevent large gaps
   */
  private cleanLineBreaks(text: string): string {
    return text
      .replace(/<br\s*\/?>\s*<br\s*\/?>/gi, ' ')
      .replace(/<br\s*\/?>/gi, ' ')
      .replace(/\n{2,}/g, ' ')
      .replace(/\n/g, ' ')
      .replace(/\s{2,}/g, ' ')
      .trim();
  }

  /**
   * Escapes HTML characters for security
   */
  escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Renders markdown tables to HTML
   */
  private renderTables(text: string): string {
    const lines = text.split('\n');
    const result: string[] = [];
    let inTable = false;
    let tableHeaders: string[] = [];
    let tableRows: string[][] = [];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i].trim();

      // Check if this is a table line (contains |)
      if (line.includes('|') && line.length > 1) {
        const cells = line.split('|')
          .map(cell => cell.trim())
          .filter(cell => cell.length > 0);

        // Check if next line is a separator line (contains dashes)
        const nextLine = i + 1 < lines.length ? lines[i + 1].trim() : '';
        const isSeparatorNext = nextLine.includes('-') && nextLine.includes('|');

        if (!inTable && isSeparatorNext) {
          // Start of table - this line is headers
          inTable = true;
          tableHeaders = cells;
          i++; // Skip the separator line
          continue;
        } else if (inTable && cells.length > 0) {
          // Table row
          tableRows.push(cells);
        } else if (inTable) {
          // End of table
          if (tableHeaders.length > 0 && tableRows.length > 0) {
            result.push(this.createHtmlTable(tableHeaders, tableRows));
          }
          inTable = false;
          tableHeaders = [];
          tableRows = [];

          // Process current line as regular text
          if (line.length > 0) {
            result.push(line);
          }
        }
      } else {
        // Not a table line
        if (inTable) {
          // End of table
          if (tableHeaders.length > 0 && tableRows.length > 0) {
            result.push(this.createHtmlTable(tableHeaders, tableRows));
          }
          inTable = false;
          tableHeaders = [];
          tableRows = [];
        }

        // Process as regular line
        result.push(line);
      }
    }

    // Handle table at end of text
    if (inTable && tableHeaders.length > 0 && tableRows.length > 0) {
      result.push(this.createHtmlTable(tableHeaders, tableRows));
    }

    return result.join('\n');
  }

  /**
   * Create HTML table from headers and rows
   */
  private createHtmlTable(headers: string[], rows: string[][]): string {
    let html = '<div class="table-responsive mt-3 mb-3 modern-table-wrapper">';
    html += '<table class="table table-sm table-striped table-bordered" style="min-width: 600px; max-width: 100%;">';

    // Headers
    html += '<thead class="table-primary"><tr>';
    for (const header of headers) {
      html += '<th class="text-center fw-bold px-2 py-1" style="font-size: 13px; white-space: normal; word-wrap: break-word;">' + header + '</th>';
    }
    html += '</tr></thead>';

    // Body
    html += '<tbody>';
    for (const row of rows) {
      html += '<tr>';
      for (let c = 0; c < headers.length; c++) {
        const cell = c < row.length ? row[c] : '';
        html += '<td class="text-start px-2 py-1" style="font-size: 12px; white-space: normal; word-wrap: break-word;">' + cell + '</td>';
      }
      html += '</tr>';
    }
    html += '</tbody>';

    html += '</table></div>';
    return html;
  }

  /**
   * Strips all HTML tags from text
   */
  stripHtml(html: string): string {
    const div = document.createElement('div');
    div.innerHTML = html;
    return div.textContent || div.innerText || '';
  }

  /**
   * Truncates text to specified length and adds ellipsis
   */
  truncate(text: string, maxLength: number): string {
    if (!text || text.length <= maxLength) return text;
    return text.substring(0, maxLength - 3) + '...';
  }

  /**
   * Convert markdown to plain text (for simple exports)
   */
  convertMarkdownToText(markdown: string): string {
    if (!markdown) return '';

    return markdown
      .replace(/#{1,6}\s*/g, '')
      .replace(/\*\*(.*?)\*\*/g, '$1')
      .replace(/\*(.*?)\*/g, '$1')
      .replace(/`(.*?)`/g, '$1')
      .replace(/```[\s\S]*?```/g, '')
      .replace(/GRAF[İI]K_HTML(?:_\d+)?:[\s\S]*?(?=\n\n|\n[A-Z]|$)/gi, '')
      .replace(/^\s*[-\*\+]\s+/gm, '• ')
      .replace(/^\s*\d+\.\s+/gm, '• ')
      .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
      .trim();
  }

  /**
   * Executes scripts within rendered HTML content
   * This should be called after inserting rendered HTML into the DOM
   */
  executeScripts(container: HTMLElement): void {
    if (!container) return;

    const scripts = container.querySelectorAll('script');

    scripts.forEach(oldScript => {
      try {
        const newScript = document.createElement('script');

        if (oldScript.src) {
          newScript.src = oldScript.src;
        } else {
          let scriptContent = oldScript.textContent || '';

          // Skip empty scripts
          if (!scriptContent.trim()) {
            return;
          }

          // Sanitize script content - remove invalid characters
          scriptContent = this.sanitizeScriptContent(scriptContent);

          // Fix color syntax: add # to hex colors that are missing it
          scriptContent = scriptContent.replace(
            /(['"])([0-9a-fA-F]{6})\1/g,
            (match, quote, hex) => quote + '#' + hex + quote
          );

          // Fix colors in object properties
          scriptContent = scriptContent.replace(
            /(color|fill|stroke|background|backgroundColor)\s*:\s*['"]?([0-9a-fA-F]{6})(?!['"]?\s*[0-9a-fA-F])/gi,
            (match, prop, hex) => {
              if (match.includes('"') || match.includes("'")) {
                return match;
              }
              return prop + ': "#' + hex + '"';
            }
          );

          // Validate script content before execution
          if (!this.isValidScript(scriptContent)) {
            // Geçersiz script sessizce atlanır - bu beklenen bir durum
            return;
          }

          // Wrap script in try-catch
          scriptContent = '(function() { try { ' + scriptContent + ' } catch(e) { /* Chart script error silently ignored */ } })();';

          newScript.textContent = scriptContent;
        }

        // Copy attributes
        Array.from(oldScript.attributes).forEach(attr => {
          if (attr.name !== 'src') {
            newScript.setAttribute(attr.name, attr.value);
          }
        });

        // Replace old script with new one
        if (oldScript.parentNode) {
          oldScript.parentNode.replaceChild(newScript, oldScript);
        }
      } catch {
        // Hatalı script elementi sessizce kaldırılır
        if (oldScript.parentNode) {
          oldScript.parentNode.removeChild(oldScript);
        }
      }
    });
  }

  /**
   * Sanitizes script content by removing invalid characters
   */
  private sanitizeScriptContent(content: string): string {
    // Remove null bytes and other control characters (except newline, tab, carriage return)
    content = content.replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, '');

    // Remove invalid Unicode characters
    content = content.replace(/[\uFFFD\uFFFE\uFFFF]/g, '');

    // Fix common encoding issues
    content = content
      .replace(/â€™/g, "'")
      .replace(/â€œ/g, '"')
      .replace(/â€/g, '"')
      .replace(/â€"/g, '-')
      .replace(/â€¦/g, '...');

    // Remove zero-width characters
    content = content.replace(/[\u200B-\u200D\uFEFF]/g, '');

    // Fix broken tooltip formatters - LLM sometimes adds extra text in strings
    content = this.fixBrokenFormatters(content);

    return content;
  }

  /**
   * Fixes broken formatter functions that LLM sometimes generates
   * Common issue: LLM adds explanation text inside string literals
   */
  private fixBrokenFormatters(content: string): string {
    // Pattern: tooltip formatter with unclosed string containing Turkish text
    // Example: return val + 'İstersen...'; } } <- missing closing quote
    
    // Fix: Replace broken tooltip y formatter with a simple one
    content = content.replace(
      /tooltip:\s*\{\s*y:\s*\{\s*formatter:\s*function\s*\([^)]*\)\s*\{[^}]*(?:İstersen|Eğer|Ayrıca|Sonraki|Örneğin)[^}]*\}\s*\}\s*\}/gi,
      "tooltip: { y: { formatter: function(val){ return Number(val).toLocaleString('tr-TR'); } } }"
    );

    // Fix unclosed strings in formatter functions
    // Pattern: return ... + 'text without closing quote; }
    content = content.replace(
      /return\s+[^;]+\+\s*['"][^'"]*(?:İstersen|Eğer|Ayrıca)[^'"]*;\s*\}\s*\}/gi,
      "return Number(val).toLocaleString('tr-TR'); } }"
    );

    // Remove any remaining LLM explanation text in string literals
    content = content.replace(
      /(['"])[^'"]*(?:İstersen|istersen|Eğer|eğer|Sonraki|sonraki adımda)[^'"]*(?!\1)/g,
      "$1"
    );

    return content;
  }

  /**
   * Validates if script content is syntactically correct
   */
  private isValidScript(content: string): boolean {
    try {
      // Try to create a function from the script content
      // This will throw if there's a syntax error
      new Function(content);
      return true;
    } catch {
      // Geçersiz syntax - sessizce false döndür
      return false;
    }
  }
}
