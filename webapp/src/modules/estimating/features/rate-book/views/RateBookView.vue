<template>
    <div class="rb-layout">
        <!-- ═══ LEFT SIDEBAR ═══════════════════════════════════════════════ -->
        <aside class="rb-sidebar" data-testid="rb-sidebar">
            <div class="rb-sidebar-header">
                <div class="rb-sidebar-title">Rate Library</div>
                <div class="rb-sidebar-sub">Client rate sheets &amp; pricing</div>
            </div>

            <div class="rb-sidebar-controls">
                <InputText
                    v-model="sidebarFilter"
                    placeholder="Search rate books…"
                    class="w-full rb-filter-input"
                    data-testid="rb-sidebar-filter"
                />
                <div class="rb-sidebar-btn-row">
                    <Button
                        label="+ New"
                        size="small"
                        data-testid="rb-new-btn"
                        @click="createNewBook"
                    />
                    <Button label="Import" size="small" outlined severity="secondary" @click="stubImport" />
                    <Button label="Export" size="small" outlined severity="secondary" @click="stubExport" />
                </div>
            </div>

            <div v-if="listLoading" class="rb-sidebar-loading">
                <ProgressSpinner style="width:24px;height:24px" />
            </div>

            <div v-else class="rb-sidebar-list" data-testid="rb-book-list">
                <template v-for="group in filteredGroups" :key="group.client">
                    <div class="rb-group-header">
                        <span>{{ group.client }}</span>
                        <span class="rb-group-count">{{ group.books.length }}</span>
                    </div>
                    <div
                        v-for="item in group.books"
                        :key="item.rateBookId"
                        class="rb-list-item"
                        :class="{ active: item.rateBookId === selectedBookId }"
                        :data-testid="`rb-item-${item.rateBookId}`"
                        @click="selectBook(item.rateBookId)"
                    >
                        <div class="rb-item-top">
                            <span class="rb-item-name">{{ item.name }}</span>
                            <span
                                class="rb-status-badge"
                                :class="isActive(item) ? 'badge-active' : 'badge-expired'"
                            >{{ isActive(item) ? 'ACTIVE' : 'EXPIRED' }}</span>
                        </div>
                        <div class="rb-item-meta">
                            {{ item.laborCount }} labor &middot; {{ item.equipmentCount }} equip
                            <template v-if="item.expiresDate">
                                &middot; Exp: {{ fmtDateShort(item.expiresDate) }}
                            </template>
                        </div>
                    </div>
                </template>
                <div v-if="filteredGroups.length === 0" class="rb-empty-list">
                    No rate books found.
                </div>
            </div>
        </aside>

        <!-- ═══ MAIN AREA ══════════════════════════════════════════════════ -->
        <main class="rb-main" data-testid="rb-main">
            <!-- Loading -->
            <div v-if="loading" class="rb-center-msg">
                <ProgressSpinner />
            </div>

            <!-- API Error -->
            <Message v-else-if="apiError" severity="error" :closable="false" class="m-3">
                Cannot reach API ({{ apiError }}). Start the API and refresh.
            </Message>

            <!-- No book (empty state) -->
            <div v-else-if="!book" class="rb-center-msg rb-no-book">
                <i class="pi pi-book rb-no-book-icon" />
                <div class="rb-no-book-title">No Rate Set Selected</div>
                <div class="rb-no-book-sub">Select a rate set from the list, or create a new one.</div>
                <Button label="+ New Rate Set" @click="createNewBook" />
            </div>

            <template v-else>
                <!-- Top toolbar -->
                <div class="rb-toolbar" data-testid="rb-toolbar">
                    <Button label="+ New Rate Set" size="small" data-testid="rb-new-rate-set" @click="createNewBook" />
                    <Button
                        label="Duplicate"
                        size="small"
                        outlined
                        severity="secondary"
                        :disabled="!book.rateBookId"
                        data-testid="rb-duplicate"
                        @click="openCloneDialog"
                    />
                    <Button
                        label="Delete"
                        size="small"
                        outlined
                        severity="danger"
                        :disabled="!book.rateBookId"
                        data-testid="rb-delete"
                        @click="openDeleteDialog"
                    />
                    <span class="rb-toolbar-spacer" />
                    <span v-if="dirty" class="rb-dirty-chip" data-testid="rb-dirty-chip">
                        <i class="pi pi-circle-fill" style="font-size:0.45rem" /> Unsaved changes
                    </span>
                    <Button
                        label="Save"
                        icon="pi pi-save"
                        :loading="saving"
                        :disabled="!dirty || !book.name?.trim()"
                        data-testid="rb-save"
                        @click="saveBook"
                    />
                </div>

                <!-- Book header -->
                <div class="rb-book-header" data-testid="rb-book-header">
                    <div class="rb-book-header-left">
                        <InputText
                            v-model="book.name"
                            class="rb-title-input"
                            placeholder="Rate Set Name"
                            data-testid="rb-name"
                            @input="markDirty"
                        />
                        <div class="rb-book-subtitle">
                            Source: {{ book.isStandardBaseline ? 'Default Rates' : (book.client || 'Custom') }}
                            <template v-if="book.city || book.state">
                                &mdash; {{ locationLabel }}
                            </template>
                        </div>
                        <div class="rb-book-dates">
                            <div class="rb-date-field">
                                <span class="rb-date-label">Effective</span>
                                <Calendar
                                    v-model="effectiveDateVal"
                                    dateFormat="yy-mm-dd"
                                    showIcon
                                    data-testid="rb-effective-date"
                                    @date-select="markDirty"
                                />
                            </div>
                            <div class="rb-date-field">
                                <span class="rb-date-label">Expires</span>
                                <Calendar
                                    v-model="expiresDateVal"
                                    dateFormat="yy-mm-dd"
                                    showIcon
                                    data-testid="rb-expires-date"
                                    @date-select="markDirty"
                                />
                            </div>
                        </div>
                    </div>
                    <div class="rb-count-badges" data-testid="rb-count-badges">
                        <span class="rb-count-badge">{{ book.laborRates.length }} LABOR</span>
                        <span class="rb-count-badge">{{ book.equipmentRates.length }} EQUIPMENT</span>
                        <span class="rb-count-badge">{{ otherCount }} OTHER</span>
                    </div>
                </div>

                <!-- Smart Lookup card -->
                <div class="rb-lookup-card">
                    <div class="rb-lookup-title">Smart Lookup</div>
                    <div class="rb-lookup-row">
                        <InputText v-model="lookup.client" placeholder="Client (required)" class="rb-lookup-field" data-testid="rb-lookup-client" />
                        <InputText v-model="lookup.city" placeholder="City" class="rb-lookup-field" data-testid="rb-lookup-city" />
                        <InputText v-model="lookup.state" placeholder="State" maxlength="2" style="width:80px" data-testid="rb-lookup-state" />
                        <Button label="Find Matches" icon="pi pi-search" size="small" :loading="lookupLoading" data-testid="rb-lookup-find" @click="findMatches" />
                        <Button label="Clear" size="small" text @click="clearMatches" />
                    </div>
                    <DataTable v-if="lookupMatches.length" :value="lookupMatches" class="mt-2" responsiveLayout="scroll" data-testid="rb-lookup-table">
                        <Column field="name" header="Rate Book" />
                        <Column field="client" header="Client" />
                        <Column field="city" header="City" />
                        <Column field="state" header="State" />
                        <Column header="Score">
                            <template #body="{ data }">
                                <Tag :value="data.score" :severity="scoreSeverity(data.score)" />
                            </template>
                        </Column>
                        <Column header="">
                            <template #body="{ data }">
                                <Button label="Load" text size="small" @click="loadMatch(data.rateBookId)" />
                            </template>
                        </Column>
                    </DataTable>
                </div>

                <!-- Tabs -->
                <TabView v-model:activeIndex="activeTab" class="rb-tabview" data-testid="rb-tabview">

                    <!-- ── LABOR ──────────────────────────────────────────── -->
                    <TabPanel header="Labor" data-testid="rb-tab-labor">
                        <div v-for="section in laborSections" :key="section.type" class="rb-labor-section">
                            <div class="rb-labor-section-header">
                                {{ section.type.toUpperCase() }} LABOR — {{ section.rows.length }} POSITIONS
                            </div>
                            <div class="rb-table-wrap">
                                <table class="rb-table" :data-testid="`rb-labor-table-${section.type}`">
                                    <thead>
                                        <tr>
                                            <th>POSITION</th>
                                            <th>TYPE</th>
                                            <th>CRAFT CODE</th>
                                            <th>NAV CODE</th>
                                            <th class="text-right">ST RATE</th>
                                            <th class="text-right">OT RATE</th>
                                            <th class="text-right">DT RATE</th>
                                            <th class="rb-act-col">ACTIONS</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <tr v-for="(row, idx) in section.rows" :key="idx">
                                            <td>{{ row.position }}</td>
                                            <td>
                                                <Tag
                                                    :value="row.laborType"
                                                    :severity="row.laborType === 'Direct' ? 'info' : 'secondary'"
                                                />
                                            </td>
                                            <td>{{ row.craftCode || '—' }}</td>
                                            <td>{{ row.navCode || '—' }}</td>
                                            <td class="text-right">{{ fmtCurrency(row.stRate) }}</td>
                                            <td class="text-right">{{ fmtCurrency(row.otRate) }}</td>
                                            <td class="text-right">{{ fmtCurrency(row.dtRate) }}</td>
                                            <td class="rb-act-col">
                                                <Button icon="pi pi-pencil" text size="small" @click="openLaborEdit(row)" />
                                                <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteLaborRow(row)" />
                                            </td>
                                        </tr>
                                        <tr v-if="section.rows.length === 0">
                                            <td colspan="8" class="rb-empty-row">No {{ section.type }} labor rates.</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div class="rb-add-bar">
                            <Button label="Add Position" icon="pi pi-plus" text size="small" data-testid="rb-add-labor" @click="openLaborAdd" />
                        </div>
                    </TabPanel>

                    <!-- ── EQUIPMENT ──────────────────────────────────────── -->
                    <TabPanel header="Equipment" data-testid="rb-tab-equipment">
                        <div class="rb-table-wrap">
                            <table class="rb-table" data-testid="rb-equipment-table">
                                <thead>
                                    <tr>
                                        <th>EQUIPMENT</th>
                                        <th class="text-right">HOURLY</th>
                                        <th class="text-right">DAILY</th>
                                        <th class="text-right">WEEKLY</th>
                                        <th class="text-right">MONTHLY</th>
                                        <th class="rb-act-col">ACTIONS</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(row, idx) in book.equipmentRates" :key="idx">
                                        <td>{{ row.name }}</td>
                                        <td class="text-right">{{ fmtCurrencyOpt(row.hourly) }}</td>
                                        <td class="text-right">{{ fmtCurrencyOpt(row.daily) }}</td>
                                        <td class="text-right">{{ fmtCurrencyOpt(row.weekly) }}</td>
                                        <td class="text-right">{{ fmtCurrencyOpt(row.monthly) }}</td>
                                        <td class="rb-act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="openEquipEdit(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteEquipRow(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="book.equipmentRates.length === 0">
                                        <td colspan="6" class="rb-empty-row">No equipment rates yet.</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <div class="rb-add-bar">
                            <Button label="+ Add Equipment" icon="pi pi-plus" text size="small" data-testid="rb-add-equip" @click="openEquipAdd" />
                        </div>
                    </TabPanel>

                    <!-- ── PER DIEM ────────────────────────────────────────── -->
                    <TabPanel header="Per Diem" data-testid="rb-tab-perdiem">
                        <div class="rb-table-wrap">
                            <table class="rb-table" data-testid="rb-perdiem-table">
                                <thead>
                                    <tr>
                                        <th>DESCRIPTION</th>
                                        <th class="text-right">RATE/DAY</th>
                                        <th class="rb-act-col">ACTIONS</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(row, idx) in perDiemRows" :key="idx">
                                        <td>{{ row.description }}</td>
                                        <td class="text-right">{{ fmtCurrency(row.rate) }}</td>
                                        <td class="rb-act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="openExpenseEdit(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteExpenseRow(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="perDiemRows.length === 0">
                                        <td colspan="3" class="rb-empty-row">No per diem rates yet.</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <div class="rb-add-bar">
                            <Button label="+ Add Per Diem" icon="pi pi-plus" text size="small" data-testid="rb-add-perdiem" @click="openExpenseAdd('PerDiem')" />
                        </div>
                    </TabPanel>

                    <!-- ── TRAVEL ─────────────────────────────────────────── -->
                    <TabPanel header="Travel" data-testid="rb-tab-travel">
                        <div class="rb-table-wrap">
                            <table class="rb-table" data-testid="rb-travel-table">
                                <thead>
                                    <tr>
                                        <th>DESCRIPTION</th>
                                        <th class="text-right">RATE</th>
                                        <th>PER</th>
                                        <th class="rb-act-col">ACTIONS</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(row, idx) in travelRows" :key="idx">
                                        <td>{{ row.description }}</td>
                                        <td class="text-right">{{ fmtCurrency(row.rate) }}</td>
                                        <td>{{ row.unit }}</td>
                                        <td class="rb-act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="openExpenseEdit(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteExpenseRow(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="travelRows.length === 0">
                                        <td colspan="4" class="rb-empty-row">No travel rates yet.</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <div class="rb-add-bar">
                            <Button label="+ Add Travel" icon="pi pi-plus" text size="small" data-testid="rb-add-travel" @click="openExpenseAdd('Travel')" />
                        </div>
                    </TabPanel>

                    <!-- ── LODGING ─────────────────────────────────────────── -->
                    <TabPanel header="Lodging" data-testid="rb-tab-lodging">
                        <div class="rb-table-wrap">
                            <table class="rb-table" data-testid="rb-lodging-table">
                                <thead>
                                    <tr>
                                        <th>DESCRIPTION</th>
                                        <th class="text-right">RATE/NIGHT</th>
                                        <th class="rb-act-col">ACTIONS</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <tr v-for="(row, idx) in lodgingRows" :key="idx">
                                        <td>{{ row.description }}</td>
                                        <td class="text-right">{{ fmtCurrency(row.rate) }}</td>
                                        <td class="rb-act-col">
                                            <Button icon="pi pi-pencil" text size="small" @click="openExpenseEdit(row)" />
                                            <Button icon="pi pi-trash" text size="small" severity="danger" @click="deleteExpenseRow(row)" />
                                        </td>
                                    </tr>
                                    <tr v-if="lodgingRows.length === 0">
                                        <td colspan="3" class="rb-empty-row">No lodging rates yet.</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                        <div class="rb-add-bar">
                            <Button label="+ Add Lodging" icon="pi pi-plus" text size="small" data-testid="rb-add-lodging" @click="openExpenseAdd('Lodging')" />
                        </div>
                    </TabPanel>
                </TabView>
            </template>
        </main>

        <!-- ═══ DIALOGS ════════════════════════════════════════════════════ -->

        <!-- Labor Edit/Add Dialog -->
        <Dialog v-model:visible="laborDialog.visible" :header="laborDialog.isNew ? 'Add Position' : 'Edit Position'" modal style="width:520px" data-testid="rb-labor-dialog">
            <div class="rb-dialog-grid">
                <div class="field-row">
                    <label class="field-label">Position *</label>
                    <InputText v-model="laborDialog.row.position" class="w-full" data-testid="rb-ld-position" />
                </div>
                <div class="field-row">
                    <label class="field-label">Type</label>
                    <Dropdown
                        v-model="laborDialog.row.laborType"
                        :options="laborTypeOptions"
                        class="w-full"
                        data-testid="rb-ld-type"
                    />
                </div>
                <div class="field-row">
                    <label class="field-label">Craft Code</label>
                    <InputText v-model="laborDialog.row.craftCode" class="w-full" data-testid="rb-ld-craft" />
                </div>
                <div class="field-row">
                    <label class="field-label">NAV Code</label>
                    <InputText v-model="laborDialog.row.navCode" class="w-full" data-testid="rb-ld-nav" />
                </div>
                <div class="field-row">
                    <label class="field-label">ST Rate</label>
                    <InputNumber v-model="laborDialog.row.stRate" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-ld-st" />
                </div>
                <div class="field-row">
                    <label class="field-label">OT Rate</label>
                    <InputNumber v-model="laborDialog.row.otRate" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-ld-ot" />
                </div>
                <div class="field-row">
                    <label class="field-label">DT Rate</label>
                    <InputNumber v-model="laborDialog.row.dtRate" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-ld-dt" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="laborDialog.visible = false" />
                <Button
                    :label="laborDialog.isNew ? 'Add' : 'Save'"
                    :disabled="!laborDialog.row.position?.trim()"
                    @click="commitLaborDialog"
                />
            </template>
        </Dialog>

        <!-- Equipment Edit/Add Dialog -->
        <Dialog v-model:visible="equipDialog.visible" :header="equipDialog.isNew ? 'Add Equipment' : 'Edit Equipment'" modal style="width:480px" data-testid="rb-equip-dialog">
            <div class="rb-dialog-grid">
                <div class="field-row rb-dialog-fullrow">
                    <label class="field-label">Equipment Name *</label>
                    <InputText v-model="equipDialog.row.name" class="w-full" data-testid="rb-ed-name" />
                </div>
                <div class="field-row">
                    <label class="field-label">Hourly</label>
                    <InputNumber v-model="equipDialog.row.hourly" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-ed-hourly" />
                </div>
                <div class="field-row">
                    <label class="field-label">Daily</label>
                    <InputNumber v-model="equipDialog.row.daily" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-ed-daily" />
                </div>
                <div class="field-row">
                    <label class="field-label">Weekly</label>
                    <InputNumber v-model="equipDialog.row.weekly" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-ed-weekly" />
                </div>
                <div class="field-row">
                    <label class="field-label">Monthly</label>
                    <InputNumber v-model="equipDialog.row.monthly" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-ed-monthly" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="equipDialog.visible = false" />
                <Button
                    :label="equipDialog.isNew ? 'Add' : 'Save'"
                    :disabled="!equipDialog.row.name?.trim()"
                    @click="commitEquipDialog"
                />
            </template>
        </Dialog>

        <!-- Expense (Per Diem / Travel / Lodging) Edit/Add Dialog -->
        <Dialog v-model:visible="expenseDialog.visible" :header="expenseDialogTitle" modal style="width:420px" data-testid="rb-expense-dialog">
            <div class="rb-dialog-grid">
                <div class="field-row rb-dialog-fullrow">
                    <label class="field-label">Description *</label>
                    <InputText v-model="expenseDialog.row.description" class="w-full" data-testid="rb-exd-desc" />
                </div>
                <div class="field-row">
                    <label class="field-label">Rate</label>
                    <InputNumber v-model="expenseDialog.row.rate" mode="currency" currency="USD" locale="en-US" class="w-full" data-testid="rb-exd-rate" />
                </div>
                <div v-if="expenseDialog.row.category === 'Travel'" class="field-row">
                    <label class="field-label">Per (unit)</label>
                    <Dropdown
                        v-model="expenseDialog.row.unit"
                        :options="travelUnitOptions"
                        class="w-full"
                        data-testid="rb-exd-unit"
                    />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="expenseDialog.visible = false" />
                <Button
                    :label="expenseDialog.isNew ? 'Add' : 'Save'"
                    :disabled="!expenseDialog.row.description?.trim()"
                    @click="commitExpenseDialog"
                />
            </template>
        </Dialog>

        <!-- Clone Dialog -->
        <Dialog v-model:visible="cloneDialog.visible" header="Duplicate Rate Set" modal style="width:520px" data-testid="rb-clone-dialog">
            <div class="rb-dialog-grid">
                <div class="field-row rb-dialog-fullrow">
                    <label class="field-label">New Book Name *</label>
                    <InputText v-model="cloneDialog.name" class="w-full" data-testid="rb-clone-name" />
                </div>
                <div class="field-row">
                    <label class="field-label">Client</label>
                    <InputText v-model="cloneDialog.client" class="w-full" />
                </div>
                <div class="field-row">
                    <label class="field-label">Client Code</label>
                    <InputText v-model="cloneDialog.clientCode" class="w-full" />
                </div>
                <div class="field-row">
                    <label class="field-label">City</label>
                    <InputText v-model="cloneDialog.city" class="w-full" />
                </div>
                <div class="field-row">
                    <label class="field-label">State</label>
                    <InputText v-model="cloneDialog.state" class="w-full" maxlength="2" />
                </div>
            </div>
            <template #footer>
                <Button label="Cancel" text @click="cloneDialog.visible = false" />
                <Button label="Duplicate" icon="pi pi-copy" :disabled="!cloneDialog.name.trim()" @click="cloneBook" />
            </template>
        </Dialog>

        <!-- Delete Confirm Dialog -->
        <Dialog v-model:visible="deleteDialog.visible" header="Delete Rate Set" modal style="width:400px" data-testid="rb-delete-dialog">
            <p>Delete <strong>{{ deleteDialog.bookName }}</strong>? This action cannot be undone.</p>
            <template #footer>
                <Button label="Cancel" text @click="deleteDialog.visible = false" />
                <Button label="Delete" severity="danger" icon="pi pi-trash" @click="confirmDelete" />
            </template>
        </Dialog>
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useApiStore } from '@/stores/apiStore';
import { useToast } from 'primevue/usetoast';

// ─── Types ───────────────────────────────────────────────────────────────────

interface RateBookListItem {
    rateBookId: number;
    name: string;
    client?: string | null;
    clientCode?: string | null;
    city?: string | null;
    state?: string | null;
    isStandardBaseline: boolean;
    effectiveDate?: string | null;
    expiresDate?: string | null;
    updatedAt?: string;
    laborCount: number;
    equipmentCount: number;
    expenseCount: number;
}

interface LaborRate {
    position: string;
    laborType: string;
    craftCode?: string | null;
    navCode?: string | null;
    stRate: number;
    otRate: number;
    dtRate: number;
}

interface EquipmentRate {
    name: string;
    hourly?: number | null;
    daily?: number | null;
    weekly?: number | null;
    monthly?: number | null;
}

interface ExpenseItem {
    category: string;
    description: string;
    rate: number;
    unit: string;
}

interface RateBook {
    rateBookId?: number;
    name: string;
    client?: string | null;
    clientCode?: string | null;
    city?: string | null;
    state?: string | null;
    isStandardBaseline: boolean;
    effectiveDate?: string | Date | null;
    expiresDate?: string | Date | null;
    updatedAt?: string;
    laborRates: LaborRate[];
    equipmentRates: EquipmentRate[];
    expenseItems: ExpenseItem[];
}

interface LookupMatch {
    rateBookId: number;
    name: string;
    client?: string | null;
    city?: string | null;
    state?: string | null;
    score: number;
}

// ─── Stores / composables ────────────────────────────────────────────────────

const apiStore = useApiStore();
const toast = useToast();

// ─── State ───────────────────────────────────────────────────────────────────

const loading = ref(false);
const listLoading = ref(false);
const saving = ref(false);
const lookupLoading = ref(false);
const dirty = ref(false);
const apiError = ref<string | null>(null);
const activeTab = ref(0);

const bookList = ref<RateBookListItem[]>([]);
const selectedBookId = ref<number | null>(null);
const book = ref<RateBook | null>(null);
const sidebarFilter = ref('');

const lookup = ref({ client: '', city: '', state: '' });
const lookupMatches = ref<LookupMatch[]>([]);

const laborTypeOptions = ['Direct', 'Indirect'];
const travelUnitOptions = ['mile', 'trip', 'day'];

// Date pickers (Calendar needs Date objects)
const effectiveDateVal = ref<Date | null>(null);
const expiresDateVal = ref<Date | null>(null);

watch(effectiveDateVal, (v) => {
    if (book.value) { book.value.effectiveDate = v; markDirty(); }
});
watch(expiresDateVal, (v) => {
    if (book.value) { book.value.expiresDate = v; markDirty(); }
});

// ─── Dialogs ─────────────────────────────────────────────────────────────────

function emptyLaborRow(): LaborRate {
    return { position: '', laborType: 'Direct', craftCode: '', navCode: '', stRate: 0, otRate: 0, dtRate: 0 };
}
function emptyEquipRow(): EquipmentRate {
    return { name: '', hourly: null, daily: null, weekly: null, monthly: null };
}
function emptyExpenseRow(cat: string): ExpenseItem {
    return { category: cat, description: '', rate: 0, unit: cat === 'Travel' ? 'mile' : cat === 'Lodging' ? 'night' : 'day' };
}

const laborDialog = ref({
    visible: false,
    isNew: true,
    originalRow: null as LaborRate | null,
    row: emptyLaborRow(),
});

const equipDialog = ref({
    visible: false,
    isNew: true,
    originalRow: null as EquipmentRate | null,
    row: emptyEquipRow(),
});

const expenseDialog = ref({
    visible: false,
    isNew: true,
    originalRow: null as ExpenseItem | null,
    row: emptyExpenseRow('PerDiem'),
});

const cloneDialog = ref({
    visible: false,
    name: '',
    client: '',
    clientCode: '',
    city: '',
    state: '',
});

const deleteDialog = ref({
    visible: false,
    bookName: '',
    bookId: 0,
});

// ─── Computed ────────────────────────────────────────────────────────────────

const filteredGroups = computed(() => {
    const filter = sidebarFilter.value.trim().toLowerCase();
    const items = filter
        ? bookList.value.filter(b =>
            b.name.toLowerCase().includes(filter) ||
            (b.client ?? '').toLowerCase().includes(filter)
          )
        : bookList.value;

    const map = new Map<string, RateBookListItem[]>();
    for (const item of items) {
        const key = item.client?.trim() || '(No Client)';
        if (!map.has(key)) map.set(key, []);
        map.get(key)!.push(item);
    }
    return Array.from(map.entries())
        .sort(([a], [b]) => a.localeCompare(b))
        .map(([client, books]) => ({ client, books }));
});

const locationLabel = computed(() => {
    if (!book.value) return '';
    const city = book.value.city?.trim();
    const state = book.value.state?.trim();
    if (city && state) return `${city}, ${state}`;
    return city || state || '';
});

const laborSections = computed(() => {
    if (!book.value) return [];
    return [
        { type: 'Direct', rows: book.value.laborRates.filter(r => r.laborType === 'Direct') },
        { type: 'Indirect', rows: book.value.laborRates.filter(r => r.laborType === 'Indirect') },
    ];
});

const perDiemRows = computed(() => book.value?.expenseItems.filter(r => r.category === 'PerDiem') ?? []);
const travelRows = computed(() => book.value?.expenseItems.filter(r => r.category === 'Travel') ?? []);
const lodgingRows = computed(() => book.value?.expenseItems.filter(r => r.category === 'Lodging') ?? []);
const otherCount = computed(() => perDiemRows.value.length + travelRows.value.length + lodgingRows.value.length);

const expenseDialogTitle = computed(() => {
    const cat = expenseDialog.value.row.category;
    const verb = expenseDialog.value.isNew ? 'Add' : 'Edit';
    if (cat === 'PerDiem') return `${verb} Per Diem`;
    if (cat === 'Travel') return `${verb} Travel`;
    return `${verb} Lodging`;
});

// ─── Helpers ─────────────────────────────────────────────────────────────────

function markDirty() { dirty.value = true; }

function isActive(item: RateBookListItem): boolean {
    if (!item.expiresDate) return true;
    return new Date(item.expiresDate) > new Date();
}

function fmtDateShort(d?: string | null): string {
    if (!d) return '';
    return d.slice(0, 10);
}

function fmtCurrency(v: number | undefined | null): string {
    if (v === null || v === undefined) return '—';
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(v);
}

function fmtCurrencyOpt(v: number | null | undefined): string {
    if (v === null || v === undefined) return '—';
    return fmtCurrency(v);
}

function scoreSeverity(score: number) {
    if (score >= 3) return 'success';
    if (score === 2) return 'info';
    return 'warning';
}

function emptyBook(): RateBook {
    return {
        name: '',
        client: '',
        clientCode: '',
        city: '',
        state: '',
        isStandardBaseline: false,
        effectiveDate: null,
        expiresDate: null,
        laborRates: [],
        equipmentRates: [],
        expenseItems: [],
    };
}

function normalizeBook(raw: any): RateBook {
    return {
        rateBookId: raw.rateBookId,
        name: raw.name ?? '',
        client: raw.client ?? '',
        clientCode: raw.clientCode ?? '',
        city: raw.city ?? '',
        state: raw.state ?? '',
        isStandardBaseline: !!raw.isStandardBaseline,
        effectiveDate: raw.effectiveDate ? raw.effectiveDate.slice(0, 10) : null,
        expiresDate: raw.expiresDate ? raw.expiresDate.slice(0, 10) : null,
        updatedAt: raw.updatedAt,
        laborRates: (raw.laborRates ?? []).map((r: any) => ({
            position: r.position ?? '',
            laborType: r.laborType ?? 'Direct',
            craftCode: r.craftCode ?? '',
            navCode: r.navCode ?? '',
            stRate: Number(r.stRate ?? 0),
            otRate: Number(r.otRate ?? 0),
            dtRate: Number(r.dtRate ?? 0),
        })),
        equipmentRates: (raw.equipmentRates ?? []).map((r: any) => ({
            name: r.name ?? '',
            hourly: r.hourly != null ? Number(r.hourly) : null,
            daily: r.daily != null ? Number(r.daily) : null,
            weekly: r.weekly != null ? Number(r.weekly) : null,
            monthly: r.monthly != null ? Number(r.monthly) : null,
        })),
        expenseItems: (raw.expenseItems ?? []).map((r: any) => ({
            category: r.category ?? 'PerDiem',
            description: r.description ?? '',
            rate: Number(r.rate ?? 0),
            unit: r.unit ?? '',
        })),
    };
}

function syncDatePickers() {
    if (!book.value) { effectiveDateVal.value = null; expiresDateVal.value = null; return; }
    effectiveDateVal.value = book.value.effectiveDate ? new Date(book.value.effectiveDate as string) : null;
    expiresDateVal.value = book.value.expiresDate ? new Date(book.value.expiresDate as string) : null;
}

// ─── API calls ────────────────────────────────────────────────────────────────

async function loadBooks() {
    const { data } = await apiStore.api.get('/api/v1/rate-books');
    bookList.value = data as RateBookListItem[];
}

async function loadBookById(id: number) {
    const { data } = await apiStore.api.get(`/api/v1/rate-books/${id}`);
    book.value = normalizeBook(data);
    syncDatePickers();
    dirty.value = false;
}

async function selectBook(id: number) {
    if (id === selectedBookId.value && book.value?.rateBookId === id) return;
    selectedBookId.value = id;
    loading.value = true;
    apiError.value = null;
    try {
        await loadBookById(id);
    } catch (err: any) {
        apiError.value = err?.code || err?.response?.status || 'UNKNOWN';
    } finally {
        loading.value = false;
    }
}

function createNewBook() {
    selectedBookId.value = null;
    book.value = emptyBook();
    syncDatePickers();
    dirty.value = false;
    activeTab.value = 0;
}

function payloadFromBook(current: RateBook) {
    return {
        name: current.name?.trim(),
        client: current.client?.trim() || null,
        clientCode: current.clientCode?.trim() || null,
        city: current.city?.trim() || null,
        state: current.state?.trim() || null,
        isStandardBaseline: current.isStandardBaseline,
        effectiveDate: current.effectiveDate ? new Date(current.effectiveDate as string).toISOString() : null,
        expiresDate: current.expiresDate ? new Date(current.expiresDate as string).toISOString() : null,
        laborRates: current.laborRates.map((r, i) => ({
            position: r.position?.trim(),
            laborType: r.laborType || 'Direct',
            craftCode: r.craftCode?.trim() || null,
            navCode: r.navCode?.trim() || null,
            stRate: Number(r.stRate || 0),
            otRate: Number(r.otRate || 0),
            dtRate: Number(r.dtRate || 0),
            sortOrder: i,
        })),
        equipmentRates: current.equipmentRates.map((r, i) => ({
            name: r.name?.trim(),
            hourly: r.hourly ?? null,
            daily: r.daily ?? null,
            weekly: r.weekly ?? null,
            monthly: r.monthly ?? null,
            sortOrder: i,
        })),
        expenseItems: current.expenseItems.map((r, i) => ({
            category: r.category,
            description: r.description?.trim(),
            rate: Number(r.rate || 0),
            unit: r.unit || '',
            sortOrder: i,
        })),
    };
}

async function saveBook() {
    if (!book.value) return;
    if (!book.value.name?.trim()) {
        toast.add({ severity: 'warn', summary: 'Validation', detail: 'Book name is required', life: 3000 });
        return;
    }
    saving.value = true;
    try {
        const payload = payloadFromBook(book.value);
        if (book.value.rateBookId) {
            await apiStore.api.put(`/api/v1/rate-books/${book.value.rateBookId}`, payload);
        } else {
            const { data } = await apiStore.api.post('/api/v1/rate-books', payload);
            book.value.rateBookId = data.rateBookId;
            selectedBookId.value = data.rateBookId;
        }
        await loadBooks();
        if (book.value.rateBookId) await loadBookById(book.value.rateBookId);
        toast.add({ severity: 'success', summary: 'Saved', detail: 'Rate book saved successfully', life: 2500 });
    } catch {
        toast.add({ severity: 'error', summary: 'Save Failed', detail: 'Could not save rate book', life: 3500 });
    } finally {
        saving.value = false;
    }
}

// ─── Delete ───────────────────────────────────────────────────────────────────

function openDeleteDialog() {
    if (!book.value?.rateBookId) return;
    deleteDialog.value = { visible: true, bookName: book.value.name, bookId: book.value.rateBookId };
}

async function confirmDelete() {
    deleteDialog.value.visible = false;
    try {
        await apiStore.api.delete(`/api/v1/rate-books/${deleteDialog.value.bookId}`);
        toast.add({ severity: 'success', summary: 'Deleted', detail: 'Rate book deleted', life: 2500 });
        await loadBooks();
        const fallback = bookList.value[0];
        if (fallback) {
            await selectBook(fallback.rateBookId);
        } else {
            createNewBook();
            book.value = null;
        }
    } catch {
        toast.add({ severity: 'error', summary: 'Delete Failed', detail: 'Could not delete rate book', life: 3500 });
    }
}

// ─── Clone ────────────────────────────────────────────────────────────────────

function openCloneDialog() {
    if (!book.value) return;
    cloneDialog.value = {
        visible: true,
        name: `${book.value.name} Copy`,
        client: book.value.client || '',
        clientCode: book.value.clientCode || '',
        city: book.value.city || '',
        state: book.value.state || '',
    };
}

async function cloneBook() {
    if (!book.value?.rateBookId) return;
    const payload = {
        name: cloneDialog.value.name?.trim(),
        client: cloneDialog.value.client?.trim() || null,
        clientCode: cloneDialog.value.clientCode?.trim() || null,
        city: cloneDialog.value.city?.trim() || null,
        state: cloneDialog.value.state?.trim() || null,
    };
    if (!payload.name) return;
    try {
        const { data } = await apiStore.api.post(`/api/v1/rate-books/${book.value.rateBookId}/clone`, payload);
        cloneDialog.value.visible = false;
        await loadBooks();
        await selectBook(data.rateBookId);
        toast.add({ severity: 'success', summary: 'Cloned', detail: `Created "${data.name}"`, life: 2500 });
    } catch {
        toast.add({ severity: 'error', summary: 'Clone Failed', detail: 'Could not clone rate book', life: 3500 });
    }
}

// ─── Smart Lookup ─────────────────────────────────────────────────────────────

async function findMatches() {
    const client = lookup.value.client.trim();
    if (!client) {
        toast.add({ severity: 'warn', summary: 'Client Required', detail: 'Enter a client name', life: 2500 });
        return;
    }
    lookupLoading.value = true;
    try {
        const { data } = await apiStore.api.get('/api/v1/rate-books/for-client', {
            params: { client, city: lookup.value.city?.trim() || null, state: lookup.value.state?.trim() || null },
        });
        lookupMatches.value = data as LookupMatch[];
        if (!lookupMatches.value.length) {
            toast.add({ severity: 'info', summary: 'No Matches', detail: 'No matching rate books found.', life: 2500 });
        }
    } catch {
        toast.add({ severity: 'error', summary: 'Lookup Failed', detail: 'Could not query rate book matches', life: 3500 });
    } finally {
        lookupLoading.value = false;
    }
}

async function loadMatch(id: number) {
    await selectBook(id);
}

function clearMatches() {
    lookupMatches.value = [];
    lookup.value = { client: '', city: '', state: '' };
}

// ─── Labor dialog ─────────────────────────────────────────────────────────────

function openLaborAdd() {
    laborDialog.value = { visible: true, isNew: true, originalRow: null, row: emptyLaborRow() };
}

function openLaborEdit(row: LaborRate) {
    laborDialog.value = { visible: true, isNew: false, originalRow: row, row: { ...row } };
}

function commitLaborDialog() {
    if (!book.value) return;
    if (laborDialog.value.isNew) {
        book.value.laborRates.push({ ...laborDialog.value.row });
    } else if (laborDialog.value.originalRow) {
        const idx = book.value.laborRates.indexOf(laborDialog.value.originalRow);
        if (idx >= 0) book.value.laborRates[idx] = { ...laborDialog.value.row };
    }
    laborDialog.value.visible = false;
    markDirty();
}

function deleteLaborRow(row: LaborRate) {
    if (!book.value) return;
    const idx = book.value.laborRates.indexOf(row);
    if (idx >= 0) { book.value.laborRates.splice(idx, 1); markDirty(); }
}

// ─── Equipment dialog ─────────────────────────────────────────────────────────

function openEquipAdd() {
    equipDialog.value = { visible: true, isNew: true, originalRow: null, row: emptyEquipRow() };
}

function openEquipEdit(row: EquipmentRate) {
    equipDialog.value = { visible: true, isNew: false, originalRow: row, row: { ...row } };
}

function commitEquipDialog() {
    if (!book.value) return;
    if (equipDialog.value.isNew) {
        book.value.equipmentRates.push({ ...equipDialog.value.row });
    } else if (equipDialog.value.originalRow) {
        const idx = book.value.equipmentRates.indexOf(equipDialog.value.originalRow);
        if (idx >= 0) book.value.equipmentRates[idx] = { ...equipDialog.value.row };
    }
    equipDialog.value.visible = false;
    markDirty();
}

function deleteEquipRow(row: EquipmentRate) {
    if (!book.value) return;
    const idx = book.value.equipmentRates.indexOf(row);
    if (idx >= 0) { book.value.equipmentRates.splice(idx, 1); markDirty(); }
}

// ─── Expense dialog ───────────────────────────────────────────────────────────

function openExpenseAdd(cat: string) {
    expenseDialog.value = { visible: true, isNew: true, originalRow: null, row: emptyExpenseRow(cat) };
}

function openExpenseEdit(row: ExpenseItem) {
    expenseDialog.value = { visible: true, isNew: false, originalRow: row, row: { ...row } };
}

function commitExpenseDialog() {
    if (!book.value) return;
    if (expenseDialog.value.isNew) {
        book.value.expenseItems.push({ ...expenseDialog.value.row });
    } else if (expenseDialog.value.originalRow) {
        const idx = book.value.expenseItems.indexOf(expenseDialog.value.originalRow);
        if (idx >= 0) book.value.expenseItems[idx] = { ...expenseDialog.value.row };
    }
    expenseDialog.value.visible = false;
    markDirty();
}

function deleteExpenseRow(row: ExpenseItem) {
    if (!book.value) return;
    const idx = book.value.expenseItems.indexOf(row);
    if (idx >= 0) { book.value.expenseItems.splice(idx, 1); markDirty(); }
}

// ─── Stubs ────────────────────────────────────────────────────────────────────

function stubImport() {
    toast.add({ severity: 'info', summary: 'Import', detail: 'Import not yet implemented', life: 2000 });
}

function stubExport() {
    toast.add({ severity: 'info', summary: 'Export', detail: 'Export not yet implemented', life: 2000 });
}

// ─── Lifecycle ────────────────────────────────────────────────────────────────

onMounted(async () => {
    listLoading.value = true;
    loading.value = true;
    apiError.value = null;
    try {
        await loadBooks();
        const def = bookList.value.find(b => b.isStandardBaseline) ?? bookList.value[0];
        if (def) {
            selectedBookId.value = def.rateBookId;
            await loadBookById(def.rateBookId);
        } else {
            book.value = emptyBook();
            syncDatePickers();
        }
    } catch (err: any) {
        apiError.value = err?.code || err?.response?.status || 'UNKNOWN';
    } finally {
        listLoading.value = false;
        loading.value = false;
    }
});
</script>

<style scoped>
/* ── Layout ────────────────────────────────────────────────────────────────── */
.rb-layout {
    display: flex;
    height: calc(100vh - 60px);
    overflow: hidden;
}

/* ── Sidebar ───────────────────────────────────────────────────────────────── */
.rb-sidebar {
    width: 260px;
    min-width: 260px;
    display: flex;
    flex-direction: column;
    background: var(--surface-card);
    border-right: 1px solid var(--surface-border);
    overflow: hidden;
}

.rb-sidebar-header {
    padding: 14px 14px 10px;
    border-bottom: 1px solid var(--surface-border);
    flex-shrink: 0;
}

.rb-sidebar-title {
    font-size: 0.95rem;
    font-weight: 700;
    color: var(--text-color);
}

.rb-sidebar-sub {
    font-size: 0.7rem;
    color: var(--text-color-secondary);
    margin-top: 2px;
}

.rb-sidebar-controls {
    padding: 8px 10px;
    border-bottom: 1px solid var(--surface-border);
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.rb-filter-input {
    font-size: 0.8rem !important;
}

.rb-sidebar-btn-row {
    display: flex;
    gap: 4px;
}

.rb-sidebar-loading {
    display: flex;
    justify-content: center;
    padding: 20px;
}

.rb-sidebar-list {
    flex: 1;
    overflow-y: auto;
    padding-bottom: 8px;
}

.rb-group-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 12px 4px;
    font-size: 0.66rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
}

.rb-group-count {
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    border-radius: 10px;
    padding: 1px 6px;
    font-size: 0.62rem;
}

.rb-list-item {
    padding: 9px 12px;
    cursor: pointer;
    border-left: 2px solid transparent;
    transition: background 0.12s;
}

.rb-list-item:hover {
    background: var(--surface-hover);
}

.rb-list-item.active {
    background: color-mix(in srgb, var(--primary-color) 12%, transparent);
    border-left-color: var(--primary-color);
}

.rb-item-top {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 4px;
}

.rb-item-name {
    font-size: 0.8rem;
    font-weight: 600;
    color: var(--text-color);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 140px;
}

.rb-status-badge {
    font-size: 0.58rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    padding: 2px 5px;
    border-radius: 3px;
    flex-shrink: 0;
}

.badge-active {
    background: color-mix(in srgb, #22c55e 15%, transparent);
    color: #16a34a;
    border: 1px solid color-mix(in srgb, #22c55e 30%, transparent);
}

.badge-expired {
    background: color-mix(in srgb, #ef4444 15%, transparent);
    color: #dc2626;
    border: 1px solid color-mix(in srgb, #ef4444 30%, transparent);
}

.rb-item-meta {
    font-size: 0.67rem;
    color: var(--text-color-secondary);
    margin-top: 2px;
}

.rb-empty-list {
    padding: 20px 14px;
    font-size: 0.78rem;
    color: var(--text-color-secondary);
    text-align: center;
    font-style: italic;
}

/* ── Main area ─────────────────────────────────────────────────────────────── */
.rb-main {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    background: var(--surface-ground);
}

.rb-center-msg {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 12px;
    flex: 1;
    color: var(--text-color-secondary);
}

.rb-no-book-icon {
    font-size: 3rem;
    color: var(--text-color-secondary);
    opacity: 0.35;
}

.rb-no-book-title {
    font-size: 1.1rem;
    font-weight: 600;
    color: var(--text-color);
}

.rb-no-book-sub {
    font-size: 0.82rem;
    color: var(--text-color-secondary);
}

/* ── Toolbar ────────────────────────────────────────────────────────────────── */
.rb-toolbar {
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 8px 14px;
    background: var(--surface-card);
    border-bottom: 1px solid var(--surface-border);
    flex-shrink: 0;
}

.rb-toolbar-spacer {
    flex: 1;
}

.rb-dirty-chip {
    display: inline-flex;
    align-items: center;
    gap: 5px;
    font-size: 0.72rem;
    font-weight: 600;
    color: var(--orange-400);
    background: color-mix(in srgb, var(--orange-400) 8%, transparent);
    border: 1px solid color-mix(in srgb, var(--orange-400) 25%, transparent);
    border-radius: 10px;
    padding: 3px 9px;
}

/* ── Book header ─────────────────────────────────────────────────────────────── */
.rb-book-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 16px;
    padding: 14px 16px 10px;
    background: var(--surface-card);
    border-bottom: 1px solid var(--surface-border);
    flex-shrink: 0;
}

.rb-book-header-left {
    display: flex;
    flex-direction: column;
    gap: 6px;
    flex: 1;
    min-width: 0;
}

.rb-title-input {
    font-size: 1.15rem !important;
    font-weight: 700 !important;
    border: none !important;
    background: transparent !important;
    padding: 0 !important;
    box-shadow: none !important;
    color: var(--text-color) !important;
}

.rb-title-input:focus {
    border-bottom: 1px solid var(--primary-color) !important;
}

.rb-book-subtitle {
    font-size: 0.75rem;
    color: var(--text-color-secondary);
}

.rb-book-dates {
    display: flex;
    gap: 16px;
    flex-wrap: wrap;
}

.rb-date-field {
    display: flex;
    align-items: center;
    gap: 6px;
}

.rb-date-label {
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
    white-space: nowrap;
}

.rb-count-badges {
    display: flex;
    flex-direction: column;
    gap: 4px;
    flex-shrink: 0;
}

.rb-count-badge {
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    background: var(--surface-ground);
    border: 1px solid var(--surface-border);
    border-radius: 4px;
    padding: 3px 10px;
    text-align: center;
    color: var(--text-color-secondary);
}

/* ── Smart lookup ─────────────────────────────────────────────────────────────── */
.rb-lookup-card {
    background: var(--surface-card);
    border-bottom: 1px solid var(--surface-border);
    padding: 8px 14px;
    flex-shrink: 0;
}

.rb-lookup-title {
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
    margin-bottom: 6px;
}

.rb-lookup-row {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
    align-items: center;
}

.rb-lookup-field {
    width: 180px;
}

/* ── TabView ─────────────────────────────────────────────────────────────────── */
.rb-tabview {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    margin: 0;
}

:deep(.rb-tabview .p-tabview-panels) {
    flex: 1;
    overflow-y: auto;
    padding: 12px 14px;
}

:deep(.rb-tabview .p-tabview-nav-container) {
    background: var(--surface-card);
    border-bottom: 1px solid var(--surface-border);
}

/* ── Tables ──────────────────────────────────────────────────────────────────── */
.rb-table-wrap {
    overflow-x: auto;
}

.rb-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.8rem;
}

.rb-table th {
    background: var(--surface-section);
    color: var(--text-color-secondary);
    font-size: 0.65rem;
    font-weight: 700;
    letter-spacing: 0.06em;
    text-align: left;
    padding: 7px 10px;
    border-bottom: 1px solid var(--surface-border);
    white-space: nowrap;
}

.rb-table td {
    padding: 7px 10px;
    border-bottom: 1px solid color-mix(in srgb, var(--surface-border) 60%, transparent);
    vertical-align: middle;
}

.rb-table tr:hover td {
    background: var(--surface-hover);
}

.rb-act-col {
    width: 80px;
    text-align: right;
    white-space: nowrap;
}

.rb-empty-row {
    text-align: center;
    color: var(--text-color-secondary);
    font-style: italic;
    padding: 16px !important;
}

/* ── Labor sections ────────────────────────────────────────────────────────── */
.rb-labor-section {
    margin-bottom: 16px;
}

.rb-labor-section-header {
    font-size: 0.7rem;
    font-weight: 700;
    letter-spacing: 0.07em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
    padding: 6px 0 4px;
    border-bottom: 1px solid var(--surface-border);
    margin-bottom: 6px;
}

/* ── Add bar ─────────────────────────────────────────────────────────────────── */
.rb-add-bar {
    margin-top: 8px;
}

/* ── Dialog grid ──────────────────────────────────────────────────────────── */
.rb-dialog-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
    padding-top: 4px;
}

.rb-dialog-fullrow {
    grid-column: 1 / -1;
}

.field-row {
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.field-label {
    font-size: 0.68rem;
    font-weight: 700;
    letter-spacing: 0.07em;
    text-transform: uppercase;
    color: var(--text-color-secondary);
}
</style>
