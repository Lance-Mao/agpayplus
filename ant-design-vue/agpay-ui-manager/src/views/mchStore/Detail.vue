<template>
  <a-drawer
    :visible="visible"
    :title=" true ? '门店详情' : '' "
    @close="onClose"
    :drawer-style="{ overflow: 'hidden' }"
    :body-style="{ paddingBottom: '80px', overflow: 'auto' }"
    width="40%"
  >
    <a-row justify="space-between" type="flex">
      <a-col :sm="10">
        <a-descriptions>
          <a-descriptions-item label="门店编号">
            {{ detailData.storeId }}
          </a-descriptions-item>
        </a-descriptions>
      </a-col>
      <a-col :sm="10">
        <a-descriptions>
          <a-descriptions-item label="门店名称">
            {{ detailData.storeName }}
          </a-descriptions-item>
        </a-descriptions>
      </a-col>
      <a-col :sm="10">
        <a-descriptions>
          <a-descriptions-item label="联系人电话">
            {{ detailData.contactPhone }}
          </a-descriptions-item>
        </a-descriptions>
      </a-col>
      <a-col :sm="10">
        <a-descriptions>
          <a-descriptions-item label="门店地址">
            {{ detailData.address }}
          </a-descriptions-item>
        </a-descriptions>
      </a-col>
      <a-col :sm="10">
        <a-descriptions>
          <a-descriptions-item label="商户号">
            {{ detailData.mchNo }}
          </a-descriptions-item>
        </a-descriptions>
      </a-col>
      <a-col :sm="10">
        <a-descriptions>
          <a-descriptions-item label="代理商号">
            {{ detailData.agentNo }}
          </a-descriptions-item>
        </a-descriptions>
      </a-col>
      <a-col :sm="10">
        <a-descriptions>
          <a-descriptions-item label="服务商号">
            {{ detailData.isvNo }}
          </a-descriptions-item>
        </a-descriptions>
      </a-col>
    </a-row>
    <a-row justify="space-between" type="flex">
      <a-col :sm="10">
        <a-form-model-item label="门店LOGO" prop="storeLogo">
          <div v-if="detailData.storeLogo">
            <a-upload
              :default-file-list="getDefaultFileList(detailData.storeLogo)"
              :showUploadList="{ showPreviewIcon:false, showRemoveIcon:false, showDownloadIcon:false }"
              list-type="picture"
              @preview="imgPreview($event)"
            />
          </div>
        </a-form-model-item>
      </a-col>
      <a-col :sm="10">
        <a-form-model-item label="门头照" prop="storeOuterImg">
          <div v-if="detailData.storeOuterImg">
            <a-upload
              :default-file-list="getDefaultFileList(detailData.storeOuterImg)"
              :showUploadList="{ showPreviewIcon:false, showRemoveIcon:false, showDownloadIcon:false }"
              list-type="picture"
              @preview="imgPreview($event)"
            />
          </div>
        </a-form-model-item>
      </a-col>
      <a-col :sm="10">
        <a-form-model-item label="门店内景照" prop="storeInnerImg">
          <div v-if="detailData.storeInnerImg">
            <a-upload
              :default-file-list="getDefaultFileList(detailData.storeInnerImg)"
              :showUploadList="{ showPreviewIcon:false, showRemoveIcon:false, showDownloadIcon:false }"
              list-type="picture"
              @preview="imgPreview($event)"
            />
          </div>
        </a-form-model-item>
      </a-col>
    </a-row>
    <a-row justify="start" type="flex">
      <a-col :sm="24">
        <a-form-model-item label="备注">
          <a-input
            type="textarea"
            disabled="disabled"
            style="height: 50px"
            v-model="detailData.remark"
          />
        </a-form-model-item>
      </a-col>
    </a-row>
  </a-drawer>
</template>

<script>
  import { API_URL_MCH_STORE, API_URL_MCH_LIST, req } from '@/api/manage'
  import 'viewerjs/dist/viewer.css'
  export default {
    props: {
      callbackFunc: { type: Function, default: () => () => ({}) }
    },
    data () {
      return {
        btnLoading: false,
        detailData: {}, // 数据对象
        recordId: null, // 更新对象ID
        visible: false, // 是否显示弹层/抽屉
        mchList: null, // 商户下拉列表
        mchName: '' // 商户名称
      }
    },
    created () {
    },
    methods: {
      show: function (recordId) { // 弹层打开事件
        if (this.$refs.infoFormModel !== undefined) {
          this.$refs.infoFormModel.resetFields()
        }
        const that = this
        that.recordId = recordId
        req.getById(API_URL_MCH_STORE, recordId).then(res => {
          that.detailData = res
        })
        req.list(API_URL_MCH_LIST, { 'pageSize': null }).then(res => { // 商户下拉选择列表
          that.mchList = res.records
          for (let i = 0; i < that.mchList.length; i++) {
            if (that.detailData.mchNo === that.mchList[i].mchNo) {
              that.mchName = that.mchList[i].mchName
            }
          }
        })
        this.visible = true
      },
      onClose () {
        this.visible = false
      },
      imgPreview (info) {
        // console.log(info)
        this.$viewerApi({
          images: [info.url],
          options: {
            initialViewIndex: 0
          }
        })
      },
      getDefaultFileList (url) {
        if (!url) {
          return []
        }
        return [{
          uid: '-1',
          name: url.split('/').pop(),
          status: 'done',
          url: url,
          thumbUrl: url
        }]
      }
    }
  }
</script>

<style lang="less">
</style>
