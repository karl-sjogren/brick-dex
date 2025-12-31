import { LitElement, html } from 'lit';

export class LegoSetsTable extends LitElement {
  static get properties() {
    return {
      sets: { type: Array },
      sortField: { type: String, state: true },
      sortDirection: { type: String, state: true },
      filterQuery: { type: String, state: true }
    };
  }

  constructor() {
    super();
    this.sets = [];
    this.sortField = 'name';
    this.sortDirection = 'asc';
    this.filterQuery = '';
  }

  // Use light DOM for easier CSS integration
  createRenderRoot() {
    return this;
  }

  get filteredAndSortedSets() {
    let result = [...this.sets];

    // Filter
    if(this.filterQuery) {
      const query = this.filterQuery.toLowerCase();
      result = result.filter(set =>
        set.name.toLowerCase().includes(query) ||
        set.setNumber.toLowerCase().includes(query) ||
        (set.themeName && set.themeName.toLowerCase().includes(query))
      );
    }

    // Sort
    result.sort((a, b) => {
      let aVal = a[this.sortField];
      let bVal = b[this.sortField];

      if(typeof aVal === 'string') {
        aVal = aVal.toLowerCase();
        bVal = bVal.toLowerCase();
      }

      if(aVal < bVal) return this.sortDirection === 'asc' ? -1 : 1;
      if(aVal > bVal) return this.sortDirection === 'asc' ? 1 : -1;
      return 0;
    });

    return result;
  }

  handleSort(field) {
    if(this.sortField === field) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDirection = 'asc';
    }
  }

  handleFilter(e) {
    this.filterQuery = e.target.value;
  }

  getSortIndicator(field) {
    if(this.sortField !== field) return '';
    return this.sortDirection === 'asc' ? ' ▲' : ' ▼';
  }

  getStatusName(status) {
    const statusNames = {
      0: null, // None
      1: 'Ordered',
      2: 'In Storage',
      3: 'Building',
      4: 'Built',
      5: 'Sold'
    };
    return statusNames[status] || null;
  }

  getStatusClass(status) {
    const statusClasses = {
      0: '',
      1: 'ordered',
      2: 'instorage',
      3: 'building',
      4: 'built',
      5: 'sold'
    };
    return statusClasses[status] || '';
  }

  render() {
    const sets = this.filteredAndSortedSets;

    return html`
      <div class="table-controls">
        <input
          type="text"
          class="form-control filter-input"
          placeholder="Filter sets..."
          .value=${this.filterQuery}
          @input=${this.handleFilter}
        />
        <span class="results-count">${sets.length} sets</span>
      </div>

      <table class="sets-table">
        <thead>
          <tr>
            <th></th>
            <th class="sortable" @click=${() => this.handleSort('setNumber')}>
              Set #${this.getSortIndicator('setNumber')}
            </th>
            <th class="sortable" @click=${() => this.handleSort('name')}>
              Name${this.getSortIndicator('name')}
            </th>
            <th class="sortable" @click=${() => this.handleSort('year')}>
              Year${this.getSortIndicator('year')}
            </th>
            <th class="sortable" @click=${() => this.handleSort('numParts')}>
              Parts${this.getSortIndicator('numParts')}
            </th>
            <th class="sortable" @click=${() => this.handleSort('themeName')}>
              Theme${this.getSortIndicator('themeName')}
            </th>
            <th>Qty</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          ${sets.map(set => html`
            <tr class="${set.status ? `has-status status-${this.getStatusClass(set.status)}` : ''}">
              <td class="image-cell">
                <div class="image-wrapper">
                  ${set.imageUrl ? html`
                    <img src="${set.imageUrl}" alt="${set.name}" class="set-thumbnail" loading="lazy" />
                  ` : html`
                    <div class="set-thumbnail-placeholder">
                      <span>No Image</span>
                    </div>
                  `}
                  ${this.getStatusName(set.status) ? html`<span class="status-badge status-${this.getStatusClass(set.status)}">${this.getStatusName(set.status)}</span>` : ''}
                </div>
              </td>
              <td>${set.setNumber}</td>
              <td>${set.name}</td>
              <td>${set.year}</td>
              <td>${(set.numParts || 0).toLocaleString()}</td>
              <td>${set.themeName || '-'}</td>
              <td>${set.quantity}</td>
              <td>
                <a href="/Sets/Details/${set.legoSetId}" class="btn btn-sm btn-secondary">Edit</a>
              </td>
            </tr>
          `)}
        </tbody>
      </table>

      ${sets.length === 0 ? html`
        <div class="empty-table-message">
          ${this.filterQuery
            ? 'No sets match your filter'
            : 'No sets in collection'}
        </div>
      ` : ''}
    `;
  }
}

customElements.define('lego-sets-table', LegoSetsTable);
